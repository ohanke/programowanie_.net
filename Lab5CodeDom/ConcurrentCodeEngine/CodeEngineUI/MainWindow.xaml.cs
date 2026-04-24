using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;

namespace CodeEngineUI
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<TaskInfo> ActiveTasks { get; set; } = new ObservableCollection<TaskInfo>();

        public MainWindow()
        {
            InitializeComponent();
            TasksListView.ItemsSource = ActiveTasks;
            LoadDefaultCode();
        }

        private void LoadDefaultCode()
        {
            CodeEditor.Text = @"using System;
using SharedLogic;
using System.Threading;

namespace DynamicCode
{
    public class TaskRunner
    {
        private static Semaphore _semaphore = new Semaphore(2, 2);

        public void Execute(int id, CriticalResource res, GlobalSolution sol)
        {
            _semaphore.WaitOne();
            try
            {
                string result = res.DoSomething(id);
                sol.UpdateBestValue(id * 1.5);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}";
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnStart.IsEnabled = false;
                BtnExit.IsEnabled = false;
                ActiveTasks.Clear();

                byte[] compiledDll = CompilerService.CompileCode(CodeEditor.Text);
                int taskCount = int.Parse(TaskCountInput.Text);
                int resLimit = int.Parse(ResourceLimitInput.Text);

                StartProcessor("TPLProcessor.exe");
                StartProcessor("ThreadProcessor.exe");

                await Task.WhenAll(
                    SendDataToPipe("TPL_Pipe", taskCount, resLimit, compiledDll, "TPL"),
                    SendDataToPipe("Thread_Pipe", taskCount, resLimit, compiledDll, "Thread")
                );

                MainProgressBar.IsIndeterminate = false;
                MainProgressBar.Value = 100;
                BtnExit.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                BtnStart.IsEnabled = true;
                BtnExit.IsEnabled = true;
            }
        }

        private void StartProcessor(string exeName)
        {
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exeName);

            if (File.Exists(localPath))
            {
                Process.Start(new ProcessStartInfo(localPath) { UseShellExecute = true });
                return;
            }

            var baseDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var solutionDir = baseDir.Parent?.Parent?.Parent?.Parent;

            if (solutionDir != null)
            {
                string projectPath = Path.Combine(solutionDir.FullName,
                    exeName.Replace(".exe", ""), "bin", "Debug", "net8.0", exeName);

                if (File.Exists(projectPath))
                {
                    Process.Start(new ProcessStartInfo(projectPath) { UseShellExecute = true });
                    return;
                }
            }

            throw new FileNotFoundException($"Błąd: Nie znaleziono pliku {exeName}. Upewnij się, że projekt został skompilowany.");
        }

        private async Task SendDataToPipe(string pipeName, int tasks, int limit, byte[] dll, string type)
        {
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
            {
                var taskEntry = new TaskInfo { Id = ActiveTasks.Count + 1, Type = type, Status = "Connecting..." };
                Application.Current.Dispatcher.Invoke(() => ActiveTasks.Add(taskEntry));

                await client.ConnectAsync(5000);
                using (var bw = new BinaryWriter(client))
                {
                    taskEntry.Status = "Sending Data...";
                    bw.Write(tasks);
                    bw.Write(limit);
                    bw.Write(dll.Length);
                    bw.Write(dll);
                    bw.Flush();
                }
                taskEntry.Status = "Running/Finished";
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            foreach (var proc in Process.GetProcessesByName("TPLProcessor")) proc.Kill();
            foreach (var proc in Process.GetProcessesByName("ThreadProcessor")) proc.Kill();
            BtnStart.IsEnabled = true;
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e) { }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

    public class TaskInfo
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}