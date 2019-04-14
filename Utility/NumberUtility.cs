using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Michael.Utility
{
    public class NumberUtility
    {
        public decimal RoundToZero(decimal number, sbyte distanceFromLastInteger)
        {
            string numberAsString = number.ToString(new CultureInfo("en-US"));
            string resultAsString = null;
            decimal result = 0;
            int roundedPosition = 0;
            sbyte roundedNumber = 0;
            decimal prependNumber = 0;

            if (distanceFromLastInteger > 0)
            {
                roundedPosition = numberAsString.Length - (1 + distanceFromLastInteger);
                roundedNumber = sbyte.Parse(numberAsString[roundedPosition].ToString());
                prependNumber = decimal.Parse(numberAsString.Substring(0, roundedPosition));
                prependNumber = roundedNumber < 5 ? prependNumber : ++prependNumber;
                resultAsString = prependNumber + Regex.Replace(numberAsString.Substring(roundedPosition), "[0-9]", "0");
            }
            else
            {
                if(!numberAsString.Contains("."))
                    throw new ArgumentException("This number doesn't have a decimal part so distance cannot be negative.");

                roundedPosition = numberAsString.LastIndexOf('.') + (-1 * distanceFromLastInteger);
                roundedNumber = sbyte.Parse(numberAsString[roundedPosition].ToString());
                prependNumber = decimal.Parse(numberAsString.Substring(0, roundedPosition), CultureInfo.InvariantCulture);
                prependNumber = roundedNumber < 5 ? prependNumber : IncrementLastDigit(prependNumber);

                if(distanceFromLastInteger != -1)
                    resultAsString = prependNumber + Regex.Replace(numberAsString.Substring(roundedPosition), "[0-9]", "0");
                else
                    resultAsString = prependNumber + "." + Regex.Replace(numberAsString.Substring(roundedPosition), "[0-9]", "0");

            }

            result = decimal.Parse(resultAsString, CultureInfo.InvariantCulture);

            return result;
        }

        public static decimal IncrementLastDigit(decimal value)
        {
            int[] bits1 = decimal.GetBits(value);
            int saved = bits1[3];
            bits1[3] = 0;   // Set scaling to 0, remove sign
            int[] bits2 = decimal.GetBits(new decimal(bits1) + 1);
            bits2[3] = saved; // Restore original scaling and sign
            return new decimal(bits2);
        }
    }
}
