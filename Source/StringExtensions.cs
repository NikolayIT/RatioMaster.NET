namespace RatioMaster_source
{
    using System;

    public static class StringExtensions
    {
        internal static double ParseDouble(this string str, double defVal)
        {
            try
            {
                System.Globalization.NumberFormatInfo ni;
                System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InstalledUICulture;
                ni = (System.Globalization.NumberFormatInfo)ci.NumberFormat.Clone();
                ni.NumberDecimalSeparator = ",";
                str = str.Replace(".", ",");
                return double.Parse(str, ni);
            }
            catch
            {
                return defVal;
            }
        }

        internal static int ParseValidInt(this string str, int defVal)
        {
            try
            {
                return int.Parse(str);
            }
            catch (Exception)
            {
                return defVal;
            }
        }

        internal static Int64 parseValidInt64(this string str, Int64 defVal)
        {
            try
            {
                return Int64.Parse(str);
            }
            catch (Exception)
            {
                return defVal;
            }
        }

        internal static float parseValidFloat(this string str, float defVal)
        {
            try
            {
                return float.Parse(str.Replace(".", ","));
            }
            catch (Exception)
            {
                return defVal;
            }
        }

        internal static string getValueDefault(this string value, string defValue)
        {
            if (value == "")
            {
                return defValue;
            }
            else
            {
                return value;
            }
        }
    }
}
