using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace PogoniumImporter
{
    class Util
    {
        public static string GetFriendlyName(string input)
        {
            input = Regex.Replace(input, @"_FAST$", "");
            input = input.ToLower();
            input = Regex.Replace(input, @"_(\w)", m => " " + m.Groups[1].Value.ToUpper());
            input = Regex.Replace(input, @"^(\w)", m => m.Groups[1].Value.ToUpper());
            return input;
        }
    }
}
