using SpriteGenerator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpriteGenerator
{
    public class Generator
    {
        private readonly CliOptions _cliOptions;
        private readonly CliExecutor _cliExecutor;

        public Generator(CliOptions cliOptions)
        {
            _cliOptions = cliOptions;
            _cliExecutor = new CliExecutor();
        }

        public void Generate()
        {
            if (!Directory.Exists(_cliOptions.DestinationPath))
            {
                Directory.CreateDirectory(_cliOptions.DestinationPath);
            }

            CreateTiles();
            var timestamps = GetFrameTimestamps();
            GenerateWebVTT(timestamps);
        }

        private string FfmpegPath => $"{_cliOptions.FfmpegPath}\\ffmpeg.exe";

        private string FfprobePath => $"{_cliOptions.FfmpegPath}\\ffprobe.exe";

        private string SpriteName => new FileInfo(_cliOptions.VideoPath).Name;

        private string VTTFileName => $"{SpriteName}_spritevtt.vtt";

        private string VTTFullPAth => $"{_cliOptions.DestinationPath}\\{SpriteName}\\{VTTFileName}";

        private List<string> SpriteFilesFullPath => Directory.GetFiles($"{_cliOptions.DestinationPath}\\{SpriteName}")
            .ToList();

        private void CreateTiles()
        {
            var spritePath = $"{_cliOptions.DestinationPath}\\{SpriteName}";
            if (!Directory.Exists(spritePath))
            {
                Directory.CreateDirectory(spritePath);
            }

            var args = $" -skip_frame nokey -i {_cliOptions.VideoPath} -vsync passthrough -vf \"scale={_cliOptions.Width}:{_cliOptions.Height},tile={_cliOptions.Columns}x{_cliOptions.Rows}\" {spritePath}/{SpriteName}_sprite%03d.jpg";
            _cliExecutor.Start(args, FfmpegPath, true);
        }

        private double[] GetFrameTimestamps()
        {
            var list = new List<double>();
            var args = $" -v error -skip_frame nokey -show_entries frame=pkt_pts_time -select_streams v -of csv=p=0 {_cliOptions.VideoPath}";
            _cliExecutor.Start(args, FfprobePath, false);
            _cliExecutor.Process.BeginOutputReadLine();
            _cliExecutor.Process.OutputDataReceived += (sender, args) =>
            {
                if (args?.Data != null && double.TryParse(args.Data, out double timestamp))
                    list.Add(timestamp);
            };
            _cliExecutor.Process.WaitForExit();

            return list.ToArray();
        }

        private void GenerateWebVTT(double[] timestamps)
        {
            var sb = new StringBuilder();
            sb.AppendLine("WEBVTT");
            sb.AppendLine("\n");

            for (int i = 0; i <= SpriteFilesFullPath.Count; i++)
            {
                var num = _cliOptions.Columns * _cliOptions.Rows * i; // 5x30x0 = 0, 5x30x1 = 150... 5x30x6 = 900
                var currentTimeStamps = timestamps.Skip(num).Take(150).ToArray();
                var listCount = currentTimeStamps.Count() - 1;

                if (listCount <= 0)
                    break;

                int rows = (int)Math.Ceiling((double)listCount / (double)_cliOptions.Columns);

                for (int r = 0; r < rows; r++)
                {
                    var calc = (listCount + 1) - r * _cliOptions.Columns;
                    int columns = calc >= _cliOptions.Columns ? _cliOptions.Columns : calc;

                    for (int c = 0; c < columns; c++)
                    {
                        var currentColumn = r * _cliOptions.Columns + c;

                        var fileName = new FileInfo(SpriteFilesFullPath[i]).Name;
                        var endpoint = $"{_cliOptions.Endpoint}/{fileName}";
                        var timestamp = currentTimeStamps[currentColumn];
                        var xywh = GetGridCoordinates(r, c);
                        var start = GetTime(timestamp);
                        var end = GetTime(timestamp, true);

                        var line = $"{start} --> {end}";
                        sb.AppendLine(line);

                        line = $"{endpoint}#xywh={xywh}";
                        sb.AppendLine(line);
                        sb.AppendLine("\n");
                    }
                }
            }

            CreateFile(VTTFullPAth, sb.ToString());
        }

        private void CreateFile(string path, string content) => File.AppendAllText(path, content);

        private string GetGridCoordinates(int currentY, int currentX)
        {
            var imgx = currentX * _cliOptions.Width;
            var imgy = currentY * _cliOptions.Height;

            return $"{imgx},{imgy},{_cliOptions.Width},{_cliOptions.Height}";
        }

        private string GetTime(double clip, bool addSecond = false)
        {
            var delta = TimeSpan.FromSeconds(clip);

            if (addSecond)
            {
                delta = delta.Add(TimeSpan.FromSeconds(1));
            }

            return string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}",
                                        (int)delta.TotalHours,
                                        delta.Minutes,
                                        delta.Seconds,
                                        delta.Milliseconds);
        }
    }
}
