using CommandLine;
using SpriteGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpriteGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var result = CommandLine.Parser.Default.ParseArguments<CliOptions>(args)
                .MapResult(OnParsedArguments, OnArgumentsError);

            Console.WriteLine("Return code = {0}", result);
        }

        private static int OnParsedArguments(CliOptions options)
        {
            try
            {
                var generator = new Generator(options);
                generator.Generate();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return -1;
            }
        }

        private static int OnArgumentsError(IEnumerable<Error> errs)
        {
            var result = -2;
            Console.WriteLine($"errors {errs.Count()}");

            if (errs.Any(x => x is HelpRequestedError || x is VersionRequestedError))
                result = -1;

            Console.WriteLine("Exit code {0}", result);

            return result;
        }
    }
}
