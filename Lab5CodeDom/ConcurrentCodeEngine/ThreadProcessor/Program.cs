using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;
using SharedLogic;

namespace ThreadProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
            {
                if (resolveArgs.Name.Contains("CodeEngineUI")) return Assembly.GetExecutingAssembly();
                return null;
            };

            using (var server = new NamedPipeServerStream("Thread_Pipe", PipeDirection.In))
            {
                server.WaitForConnection();
                using (var br = new BinaryReader(server))
                {
                    int maxTasks = br.ReadInt32();
                    int resLimit = br.ReadInt32();
                    int dllSize = br.ReadInt32();
                    byte[] dllBytes = br.ReadBytes(dllSize);

                    var resource = new CriticalResource(resLimit);
                    var solution = new GlobalSolution();
                    var assembly = Assembly.Load(dllBytes);
                    var type = assembly.GetType("DynamicCode.TaskRunner")!;
                    var instance = Activator.CreateInstance(type)!;
                    var method = type.GetMethod("Execute")!;

                    using (var countdown = new CountdownEvent(maxTasks))
                    {
                        for (int i = 0; i < maxTasks; i++)
                        {
                            int id = i;
                            ThreadPool.QueueUserWorkItem(_ =>
                            {
                                try
                                {
                                    method.Invoke(instance, new object[] { id, resource, solution });
                                    Console.WriteLine($"Thread {id} finished.");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Thread {id} error: {ex.InnerException?.Message}");
                                }
                                finally { countdown.Signal(); }
                            });
                        }
                        countdown.Wait();
                    }
                }
            }
            Console.WriteLine("All threads finished. Press any key to exit.");
            Console.ReadKey();
        }
    }
}