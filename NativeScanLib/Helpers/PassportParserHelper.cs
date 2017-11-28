using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NativeScanLib.Models;

namespace NativeScanLib.Helpers
{
    public class PassportParserHelper
    {
        public static string StripPadding(string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                return Regex.Replace(str, "<", " ");
            }

            return null;
        }

        public static Names GetNames(string str)
        {
            string[] stringSeparators = new[] { "<<" };
            var names = str.Split(stringSeparators, StringSplitOptions.None);

            return new Names
            {
                LastName = StripPadding(names[0]),
                FirstNames = StripPadding(names[1]).Split(' ')
            };
        }

        public static DateTime GetFullDate(string str)
        {
            var regex = new Regex(@"(?<year>\d{2})(?<month>\d{2})(?<day>\d{2})");
            var match = regex.Match(str);

            var year = int.Parse(match.Groups["year"].Value);
            var month = int.Parse(match.Groups["month"].Value);
            var day = int.Parse(match.Groups["day"].Value);

            var date = new DateTime(
                year >= 60 ? 1900 + year : 2000 + year,
                month,
                day
            );
            return date;
        }

        public static Region GetRegion(string str)
        {
            var country = Dictionaries.Countries.CountryDictionary[str.Replace("<", string.Empty)];
            return new Region
            {
                Key = str,
                Value = !string.IsNullOrEmpty(country) ? country : "Unknown",
            };
        }

        public static bool CheckDigitVerify(string str, int digit)
        {
            int curWeight = 0;
            int total = 0;
            var nmbrs = new List<int>();
            var weighting = new[] { 7, 3, 1 };

            for (var i = 0; i < str.Length; i++)
            {
                var character = str.Substring(i, 1);
                if(Regex.IsMatch(character, "([A-Za-z<])"))
                {
                    nmbrs.Add(Dictionaries.CheckDigits.CheckDigitsDictionary[character]);
                } 
                else 
                {
                    nmbrs.Add(int.Parse(character));
                }

            }

            for (var j = 0; j < nmbrs.Count; j++)
            {
                total += (nmbrs[j] * weighting[curWeight]);
                curWeight++;

                if (curWeight == 3)
                {
                    curWeight = 0;
                }
            }

            return ((total % 10) == digit);
        }
    }
}
