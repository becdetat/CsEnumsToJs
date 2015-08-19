using System;
using System.Linq;

namespace CsEnumsToJs
{
    public static partial class StringExtensions
    {
        public static bool IsRoughly(this string input, params string[] matches)
        {
            return matches.Any(match => input.Trim().Equals(match.Trim(), StringComparison.InvariantCultureIgnoreCase));
        }

        public static string ToCamelCase(this string input)
        {
            return input.First().ToString().ToLower() + input.Substring(1);
        }
    }
}