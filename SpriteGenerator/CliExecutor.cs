using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SpriteGenerator
{
    public class CliExecutor
    {
        public Process Process { get; set; }

        public Action<string> OnDataReceived { get; set; }

        public void Start(string args, string file = null)
        {
            var info = new ProcessStartInfo()
            {
                FileName = file ?? "cmd.exe",
                Arguments = args,
                CreateNoWindow = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process = Process.Start(info);
            Process.WaitForExit();
            
            if (Process.ExitCode != 0)
            {
                throw new ArgumentException();
            }
        }

        public void Start(string args, string process, bool wait)
        {
            Start(args, process);
            if (wait)
            {
                Process.WaitForExit();

                if (Process.ExitCode != 0)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
