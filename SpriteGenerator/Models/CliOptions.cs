using CommandLine;

namespace SpriteGenerator.Models
{
    public class CliOptions
    {
        [Option('v', "videoPath", Required = true)]
        public string VideoPath { get; set; }

        [Option('e', "endpoint", Required = true, HelpText = "Endpoint of the generated sprite images")]
        public string Endpoint { get; set; }

        [Option('w', "width", Required = false, Default = 120)]
        public int Width { get; set; }

        [Option('h', "height", Required = false, Default = 80)]
        public int Height { get; set; }

        [Option('r', "rows", Required = false, Default = 30)]
        public int Rows { get; set; }

        [Option('c', "columns", Required = false, Default = 5)]
        public int Columns { get; set; }

        [Option('f', "ffmpeg", Required = false, Default = ".\\ffmpeg", HelpText = "Base directory of ffmpeg")]
        public string FfmpegPath { get; set; }

        [Option('m', "montage", Required = false, Default = ".\\montage", HelpText = "Base directory of montage")]
        public string MontagePath { get; set; }

        [Option('d', "destination", Required = false, Default = ".\\sprite")]
        public string DestinationPath { get; set; }
    }
}
