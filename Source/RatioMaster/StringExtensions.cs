namespace RatioMaster_source
{
    using System;
    using System.Globalization;

    public static class StringExtensions
    {
        internal static double ParseDouble(this string inputString, double defVal)
        {
            try
            {
                var ci = CultureInfo.InstalledUICulture;
                var numberFormatInfo = (NumberFormatInfo)ci.NumberFormat.Clone();
                numberFormatInfo.NumberDecimalSeparator = ",";
                inputString = inputString.Replace(".", ",");
                return double.Parse(inputString, numberFormatInfo);
            }
            catch
            {
                return defVal;
            }
        }

        internal static int ParseValidInt(this string inputString, int defVal)
        {
            try
            {
                return int.Parse(inputString);
            }
            catch (Exception)
            {
                return defVal;
            }
        }

        internal static long ParseValidInt64(this string inputString, long defaultValue)
        {
            try
            {
                return long.Parse(inputString);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        internal static float ParseValidFloat(this string inputString, float defaultValue)
        {
            try
            {
                return float.Parse(inputString.Replace(".", ","));
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        internal static string GetValueDefault(this string inputString, string defaultValue)
        {
            if (inputString == string.Empty)
            {
                return defaultValue;
            }
            
            return inputString;
        }
    }
}
