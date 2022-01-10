using SpriteGenerator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
                Directory.CreateDirectory(_cliOptions.DestinationPath);

            VideoId = Guid.NewGuid();
            var frameRate = GetVideoFrameRate();
            CreateFrameImages();
            var timestamps = ParseTimeStamps(frameRate);
            GenerateTile();
            GenerateWebVTT(timestamps);
        }

        private string FfmpegPath => $"{_cliOptions.FfmpegPath}\\ffmpeg.exe";

        private string FfprobePath => $"{_cliOptions.FfmpegPath}\\ffprobe.exe";

        private string VTTFileName => $"{VideoId}_spritevtt.vtt";

        private string VTTFullPAth => $"{_cliOptions.DestinationPath}\\{VideoId}\\{VTTFileName}";

        private string MontagePath => $"{_cliOptions.MontagePath}\\montage.exe";

        private Guid VideoId { get; set; }

        private void CreateFrameImages()
        {
            var spritePath = $"{_cliOptions.DestinationPath}\\{VideoId}";
            if (Directory.Exists(spritePath))
                Directory.Delete(spritePath, true);

            Directory.CreateDirectory(spritePath);

            var args = $" -loglevel panic -skip_frame nokey -i {_cliOptions.VideoPath} -q:v 5 -vf \"scale={_cliOptions.Width}:{_cliOptions.Height}\" -vsync 0 -frame_pts 1 {spritePath}/framets-%d.jpg";
            _cliExecutor.Start(args, FfmpegPath, true);
        }

        private float GetVideoFrameRate()
        {
            var args = $"-v error -select_streams v -of default=noprint_wrappers=1:nokey=1 -show_entries stream=r_frame_rate {_cliOptions.VideoPath}";
            var result = FfmpegUtils.GetFfprobeSingleOutput(args, FfprobePath);

            return MathUtils.FractionToDecimal(result);
        }

        private List<string> SpriteFilesFullPath()
        {
            var spritePath = $"{_cliOptions.DestinationPath}\\{VideoId}";
            var fileNames = Directory.GetFiles(spritePath).ToList();

            fileNames = fileNames.Select(f => new FileInfo(f).Name).OrderByAlphaNumeric(x => x).ToList();

            return fileNames;
        }

        private float[] ParseTimeStamps(float frameRate)
        {
            var fileNames = SpriteFilesFullPath();
            var frameTimestamps = fileNames.Select(f =>
            {
                var match = Regex.Match(f, "(?<=framets-)\\d+");
                if (match.Success)
                    return match.Value;
                else
                    return null;
            }).Where(f => f != null).ToList();

            return frameTimestamps.Select(f => int.Parse(f) / frameRate)
                .ToArray();
        }

        private void GenerateTile()
        {
            var spritePath = $"{Path.GetFullPath(_cliOptions.DestinationPath)}\\{VideoId}";
            var frameTxtPath = CreateMontageFileOrder();

            var path = $"{spritePath}/{VideoId}_%d.jpg";
            var args = $"-mode concatenate -tile {_cliOptions.Columns}x{_cliOptions.Rows} @frames.txt {path}";
            
            _cliExecutor.Start(args, MontagePath, true, spritePath);

            // cleanup
            if (File.Exists(frameTxtPath))
                File.Delete(frameTxtPath);

            var dir = new DirectoryInfo(spritePath);
            foreach (var file in dir.EnumerateFiles("framets-*.jpg"))
                file.Delete();
        }

        private string CreateMontageFileOrder()
        {
            var spritePath = $"{_cliOptions.DestinationPath}\\{VideoId}";
            var tempTxt = $"{spritePath}/frames.txt";

            if (File.Exists(tempTxt))
                File.Delete(tempTxt);

            var fileNames = SpriteFilesFullPath().Select(Path.GetFileName).ToList();
            File.AppendAllLines(tempTxt, fileNames);

            return tempTxt;
        }

        private void GenerateWebVTT(float[] timestamps)
        {
            var sb = new StringBuilder();
            sb.AppendLine("WEBVTT");
            sb.AppendLine("\n");

            for (int i = 0; i <= SpriteFilesFullPath().Count; i++)
            {
                var num = _cliOptions.Columns * _cliOptions.Rows * i; // 5x30x0 = 0, 5x30x1 = 150... 5x30x6 = 900
                var currentTimeStamps = timestamps.Skip(num).Take(150).ToArray();
                var listCount = currentTimeStamps.Count() - 1;

                if (listCount <= 0)
                    break;

                int rows = (int)Math.Ceiling((double)listCount / (double)_cliOptions.Columns);

                for (int r = 0; r < rows; r++)
                {
                    var calc = listCount + 1 - (r * _cliOptions.Columns);
                    int columns = calc >= _cliOptions.Columns ? _cliOptions.Columns : calc;

                    for (int c = 0; c < columns; c++)
                    {
                        var currentColumn = r * _cliOptions.Columns + c;

                        var fileName = new FileInfo(SpriteFilesFullPath()[i]).Name;
                        var endpoint = $"{_cliOptions.Endpoint}/{fileName}";
                        var timestamp = currentTimeStamps[currentColumn];

                        var nextFrameIndex = currentColumn + 1;
                        double? nextFrame;

                        if (nextFrameIndex > currentTimeStamps.Length - 1)
                        {
                            // keyframes have equal time differences per video?
                            var previousIndex = currentColumn - 1;
                            var previousFrame = currentTimeStamps[previousIndex];
                            nextFrame = timestamp + (timestamp - previousFrame);
                        }
                        else
                        {
                            nextFrame = currentTimeStamps[nextFrameIndex];
                        }

                        var xywh = GetGridCoordinates(r, c);
                        var start = GetTime(timestamp, null);
                        var end = GetTime(timestamp, nextFrame);

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

        private string GetTime(double clip, double? end)
        {
            var delta = TimeSpan.FromSeconds(clip);

            if (end != null)
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
