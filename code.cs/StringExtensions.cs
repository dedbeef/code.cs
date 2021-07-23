using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace code.cs
{
    static class StringExtensions
    {
        public static string ToFilename(this string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        public static string ToPascal(this string name)
        {
            var result = new string(name.ToLower().Select(x =>
            {
                if (char.IsLetterOrDigit(x))
                {
                    return x;
                }
                else
                {
                    return ' ';
                }
            }).ToArray());
            var info = CultureInfo.CurrentCulture.TextInfo;
            result = info.ToTitleCase(result).Replace(" ", "");
            if(char.IsDigit(result[0]))
            {
                result = "_" + result;
            }
            return result;
        }
    }
}
