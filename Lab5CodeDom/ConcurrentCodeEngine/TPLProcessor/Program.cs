using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading.Tasks;
using SharedLogic;

namespace TPLProcessor
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

            using (var server = new NamedPipeServerStream("TPL_Pipe", PipeDirection.In))
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

                    Parallel.For(0, maxTasks, i =>
                    {
                        try
                        {
                            method.Invoke(instance, new object[] { i, resource, solution });
                            Console.WriteLine($"Task {i} finished. Best: {solution.GetBestValue()}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Task {i} error: {ex.InnerException?.Message ?? ex.Message}");
                        }
                    });
                }
            }
            Console.WriteLine("Processing finished. Press any key to exit.");
            Console.ReadKey();
        }
    }
}