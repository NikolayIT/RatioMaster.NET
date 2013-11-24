using System;
using System.Text;

namespace RatioMaster_source
{
    internal class RandomStringGenerator
    {
        // Fields
        private char[] characterArray;
        Random randNum = new Random();
        // Methods
        public RandomStringGenerator()
        {
            characterArray = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        }
        public char GetRandomCharacter()
        {
            return characterArray[(int)((characterArray.GetUpperBound(0) + 1) * randNum.NextDouble())];
        }
        public string Generate(int stringLength)
        {
            return Generate(stringLength, false);
        }
        public string Generate(int stringLength, bool randomness)
        {
            StringBuilder sb = new StringBuilder();
            sb.Capacity = stringLength;
            for (int count = 0; count <= stringLength - 1; count++)
            {
                if (randomness)
                {
                    sb.Append((char)(randNum.Next(255)));
                }
                else
                {
                    sb.Append(GetRandomCharacter());
                }
            }
            if (sb != null)
            {
                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        public string Generate(int stringLength, char[] characterArray)
        {
            StringBuilder sb = new StringBuilder();
            sb.Capacity = stringLength;
            for (int count = 0; count <= stringLength - 1; count++)
            {
                sb.Append(characterArray[(int)((characterArray.GetUpperBound(0) + 1) * randNum.NextDouble())]);
            }
            if (sb != null)
            {
                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        public string Generate(string Str, bool upperCase)
        {
            string ret = "";
            string temp = "";
            for (int i = 0; i < Str.Length; i = i + 1)
            {
                if (Char.IsLetterOrDigit(Str[i]) && (char)Str[i] < 127)
                {
                    ret += Str[i];
                }
                else
                {
                    ret += "%";
                    temp = Convert.ToString((char)Str[i], 16);
                    if (upperCase)
                    {
                        temp = temp.ToUpper();
                    }
                    if (temp.Length == 1)
                    {
                        ret += "0" + temp;
                    }
                    else
                    {
                        ret += temp;
                    }
                }
            }
            return ret;
        }
    }
}
