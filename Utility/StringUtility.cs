using System;

namespace Michael.Utility
{
    public class StringUtility
    {
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
