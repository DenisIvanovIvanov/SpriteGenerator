using System;
using System.Collections.Generic;
using System.Text;

namespace SpriteGenerator
{
    public static class FfmpegUtils
    {
        public static string GetFfprobeSingleOutput(string args, string ffprobeExePath)
        {
            string result = default;
            var cliExecuter = new CliExecutor();

            cliExecuter.Start(args, ffprobeExePath, false);
            cliExecuter.Process.BeginOutputReadLine();
            cliExecuter.Process.OutputDataReceived += (sender, args) =>
            {
                if (string.IsNullOrEmpty(args?.Data))
                    Console.WriteLine("Ffprobe empty output found.");

                if (args?.Data != null)
                    result = args.Data;
            };
            cliExecuter.Process.WaitForExit();

            return result;
        }
    }
}
