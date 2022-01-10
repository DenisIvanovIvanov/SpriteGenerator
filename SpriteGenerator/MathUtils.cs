using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpriteGenerator
{
    public static class MathUtils
    {
        public static float FractionToDecimal(string input)
        {
            if (string.IsNullOrEmpty(input))
                input = "30000/1000"; // should not be empty, but set default to 30. 

            string[] fraction = input.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            if (fraction.Length != 2)
            {
                throw new ArgumentOutOfRangeException();
            }
            int numerator, denominator;
            if (int.TryParse(fraction[0], out numerator) && int.TryParse(fraction[1], out denominator))
            {
                if (denominator == 0)
                {
                    throw new InvalidOperationException("Divide by 0 occurred");
                }
                return (float)numerator / denominator;
            }
            throw new ArgumentException();
        }

        public static IOrderedEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> source, Func<T, string> selector)
        {
            int max = source
                .SelectMany(i => Regex.Matches(selector(i), @"\d+").Cast<Match>().Select(m => (int?)m.Value.Length))
                .Max() ?? 0;

            return source.OrderBy(i => Regex.Replace(selector(i), @"\d+", m => m.Value.PadLeft(max, '0')));
        }
    }
}
