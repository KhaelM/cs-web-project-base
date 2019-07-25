using System;
using System.Text.RegularExpressions;

namespace Michael.Utility
{
    public class StringUtility
    {
        public static string FromKebabToPascal(string source)
        {
            string[] names = source.Split('_');
            string result = "";
            foreach (string name in names)
            {
                result += FirstUpper(name, false);
            }

            return result;
        }

        public static string FromKebabToCamel(string source)
        {
            string[] names = source.Split('_');
            string result = "";
            foreach (string name in names)
            {
                result += FirstUpper(name, false);
            }

            return result.Substring(0,1).ToLower() + result.Substring(1);
        }

        public static string FromPascalToKebab(string source)
        {
            return FromCamelToKebab(source);
        }

        public static string FromPascalToCamel(string source)
        {
            return source.Substring(0, 1).ToLower() + source.Substring(1);
        }

        public static string FromCamelToPascal(string source)
        {
            return FirstUpper(source, false);
        }

        public static string FromCamelToKebab(string source)
        {
            string pattern = "([a-z])([A-Z]+)";
            string replacement = "$1_$2";

            return Regex.Replace(source, pattern, replacement).ToLower();
        }

        public static string FirstUpper(string source, bool isRestLower)
        {
            if(isRestLower)
            {
                return source.Substring(0, 1).ToUpper() + source.Substring(1).ToLower();
            }
            return source.Substring(0, 1).ToUpper() + source.Substring(1);
        }

        public static bool IsListIncluded(string[] smallArray, string[] bigArray)
        {
            for (int i = 0; i < smallArray.Length; i++)
            {
                for (int j = 0; j < bigArray.Length; j++)
                {
                    if (smallArray[i].Equals(bigArray[j], StringComparison.InvariantCultureIgnoreCase))
                        break;
                    if (j == bigArray.Length - 1 && !smallArray[i].Equals(bigArray[j], StringComparison.InvariantCultureIgnoreCase))
                        return false;
                }
            }
            return true;
        }

        public static string ToPascalCase(string source)
        {
            return source.Substring(0, 1).ToUpper() + source.Substring(1);
        }
    }
}
