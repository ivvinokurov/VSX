using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO.Compression;
using System.IO;
using System.Windows.Forms;

namespace VStorage
{
    static public class VSLib
    {

        /// <summary>
        /// Parse string with delimiters to the string array
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delimiters"></param>
        /// <returns></returns>
        static public string[] Parse(string value, string delimiters = "/")
        {
            // sample "abc/aa" "/abc/aa" "aa"
            char[] delimiterChars = new char[delimiters.Length];
            for (int i = 0; i < delimiters.Length; i++)
                delimiterChars[i] = Convert.ToChar(delimiters.Substring(i,1));

            string s = value.Trim();

            if (s.Length == 0)
                return new string[0];

            for (int i = 0; i < delimiterChars.Length; i++ )
            {
                if (s.Substring(0, 1) == delimiters.Substring(i, 1))
                {
                    s = s.Remove(0, 1);
                    break;
                }
            }

            for (int i = 0; i < delimiterChars.Length; i++)
            {
                if (s.Substring(s.Length - 1, 1) == delimiters.Substring(i, 1))
                {
                    s = s.Remove(s.Length - 1, 1);
                    break;
                }
            }


            return s.Trim().Split(delimiterChars);
        }

        /// <summary>
        /// Compare to strings, 2nd is a pattern and can include wildcards '*' and '?'
        /// </summary>
        /// <param name="value"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        static public bool Compare(string pattern, string value)
        {
            if (String.Compare(pattern, value) == 0)
            {
                return true;
            }
            else if (String.IsNullOrEmpty(value))
            {
                if (String.IsNullOrEmpty(pattern.Trim(new Char[1] { '*' })))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (pattern.Length == 0)
            {
                return false;
            }
            else if (pattern[0] == '?')
            {
                return Compare(pattern.Substring(1), value.Substring(1));
            }
            else if (pattern[pattern.Length - 1] == '?')
            {
                return Compare(pattern.Substring(0, pattern.Length - 1), value.Substring(0, value.Length - 1));
            }
            else if (pattern[0] == '*')
            {
                if (Compare(pattern.Substring(1), value))
                {
                    return true;
                }
                else
                {
                    return Compare(pattern, value.Substring(1));
                }
            }
            else if (pattern[pattern.Length - 1] == '*')
            {
                if (Compare(pattern.Substring(0, pattern.Length - 1), value))
                {
                    return true;
                }
                else
                {
                    return Compare(pattern, value.Substring(0, value.Length - 1));
                }
            }
            else if (pattern[0] == value[0])
            {
                return Compare(pattern.Substring(1), value.Substring(1));
            }
            return false;
        }

        /////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////// DATA CONVERSION //////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Convert byte array to string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ConvertByteToString(byte[] value)
        {
            return ConvertByteToString(value, 0, value.Length); 
        }

        /// <summary>
        /// Convert part of byte array to string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ConvertByteToString(byte[] value, int index, int length)
        {
            return System.Text.Encoding.Default.GetString(value, index, length);
        }

        /// <summary>
        /// Convert string to byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ConvertStringToByte(string value)
        {
            return System.Text.Encoding.Default.GetBytes(value);
        }

        /// <summary>
        /// Convert string to long
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static long ConvertStringToLong(string value)
        {
            string v = ((value == "") ? "0" : value);
            return Convert.ToInt64(v);
        }

        /// <summary>
        /// Convert string to int
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ConvertStringToInt(string value)
        {
            return Convert.ToInt32(value);
        }

        /// <summary>
        /// Convert string to float
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float ConvertStringToFloat(string value)
        {
            return (float)Convert.ToDouble(value);
        }

        /// <summary>
        /// Convert string to double
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ConvertStringToDouble(string value)
        {
            return Convert.ToDouble(value);
        }

        /// <summary>
        /// Convert byte array to long
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static long ConvertByteToLong(byte[] value)
        {
            return BitConverter.ToInt64(value, 0);
        }

        /// <summary>
        /// Convert byte array to long (reverse)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static long ConvertByteToLongReverse(byte[] value)
        {
            byte[] b = new byte[value.Length];
            for (int i = 0; i < b.Length; i++)
                b[b.Length - i - 1] = value[i];

            return BitConverter.ToInt64(b, 0);
        }


        /// <summary>
        /// Convert long to byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ConvertLongToByte(long value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// Convert long to byte array in revese order (high first)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ConvertLongToByteReverse(long value)
        {
            byte[] b = BitConverter.GetBytes(value);
            byte[] ret = new byte[b.Length];
            for (int i = 0; i < b.Length; i++)
                ret[b.Length - i - 1] = b[i];
            return ret;
        }

        /// <summary>
        /// Convert int to byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ConvertIntToByte(int value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// Convert uint to byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ConvertUIntToByte(uint value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// Convert ulong to byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ConvertULongToByte(ulong value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// Convert byte array to int
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ConvertByteToInt(byte[] value)
        {
            return BitConverter.ToInt32(value, 0);
        }

        /// <summary>
        /// Convert byte array to uint
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static uint ConvertByteToUInt(byte[] value)
        {
            return BitConverter.ToUInt32(value, 0);
        }

        /// <summary>
        /// Convert byte array to ulong
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ulong ConvertByteToULong(byte[] value)
        {
            return BitConverter.ToUInt64(value, 0);
        }

        /// <summary>
        /// Convert short to byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ConvertShortToByte(short value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// Convert ushort to byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ConvertUShortToByte(ushort value)
        {
            return BitConverter.GetBytes(value);
        }


        /// <summary>
        /// Convert byte array to short
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static short ConvertByteToShort(byte[] value)
        {
            return BitConverter.ToInt16(value, 0);
        }

        /// <summary>
        /// Convert byte array to u-short
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ushort ConvertByteToUShort(byte[] value)
        {
            return BitConverter.ToUInt16(value, 0);
        }

        /// <summary>
        /// Convert long to hex representation
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ConvertLongToHexString(long value)
        {
            string st = "";
            byte[] bytes = BitConverter.GetBytes(value);
            for (int i = bytes.Length - 1; i >= 0; i--)
                st += bytes[i].ToString("X2");
            return st;
        }

        /// <summary>
        /// Convert ulong to hex representation
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ConvertULongToHexString(ulong value)
        {
            string st = "";
            byte[] bytes = BitConverter.GetBytes(value);
            for (int i = bytes.Length - 1; i >= 0; i--)
                st += bytes[i].ToString("X2");
            return st;
        }

        /// <summary>
        /// Convert uint to hex representation
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ConvertUIntToHexString(uint value)
        {
            string st = "";
            byte[] bytes = BitConverter.GetBytes(value);
            for (int i = bytes.Length - 1; i >= 0; i--)
                st += bytes[i].ToString("X2");
            return st;
        }

        /// <summary>
        /// Convert string to hex representation
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ConvertStringToHexString(string value)
        {
            string st = "";
            byte[] bytes = ConvertStringToByte(value);
            foreach (byte b in bytes)
                st += b.ToString("X2");
            return st;
        }

        /// <summary>
        /// Convert int to hex representation
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ConvertIntToHexString(int value)
        {
            string st = "";
            byte[] bytes = BitConverter.GetBytes(value);
            for (int i = bytes.Length - 1; i >= 0; i--)
                st += bytes[i].ToString("X2");
            return st;
        }

        /// <summary>
        /// Write setting
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static void VSSetKey(string key, string value)
        {
            if (!Directory.Exists(DEFS.KEY_DIRECTORY))
                Directory.CreateDirectory(DEFS.KEY_DIRECTORY);

            File.WriteAllText(DEFS.KEY_DIRECTORY + "\\" + key, value);

        }

        /// <summary>
        /// Read setting
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string VSGetKey(string key)
        {
            if (!Directory.Exists(DEFS.KEY_DIRECTORY))
                Directory.CreateDirectory(DEFS.KEY_DIRECTORY);

            if (File.Exists(DEFS.KEY_DIRECTORY + "\\" + key))
                return File.ReadAllText(DEFS.KEY_DIRECTORY + "\\" + key).Trim();
            else
                return "";

        }

        /// <summary>
        /// Copy bytes from one array to another
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <param name="target_offset"></param>
        /// <param name="length"></param>
        public static void CopyBytes(byte[] target, byte[] source, int target_offset = 0, int length = 0)
        {
            if ((length == 0) | (source.Length == 0) | (target.Length == 0) | (length > source.Length) | (target_offset + length > target.Length))
                throw new VSException(DEFS.E0028_INVALID_LENGTH_ERROR_CODE, " (GetByteArray)");

            for (int i = 0; i < length; i++)
                target[target_offset + i] = source[i];
        }

        /// <summary>
        /// Return sub-array of bytes
        /// </summary>
        /// <param name="source"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] GetByteArray(byte[] source, int offset, int length)
        {

            if ((length == 0) | (source.Length == 0) | (offset + length > source.Length))
                throw new VSException(DEFS.E0028_INVALID_LENGTH_ERROR_CODE, " (GetByteArray)");

            byte[] b = new byte[length];

            for (int i = 0; i < length; i++)
                b[i] = source[offset + i];

            return b;
        }

        /// <summary>
        /// Check if string represents a numeric value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNumeric(string value)
        {
            try
            {
                double d = Convert.ToDouble(value);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Compare byte[] keys
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="partial">true - "starts with"</param>
        /// <returns></returns>
        public static int CompareKeys(byte[] x, byte[] y, bool partial = false)
        {
            int l = Math.Min(x.Length, y.Length);

            for (int i = 0; i < l; i++)
            {
                if (x[i] > y[i])
                    return 1;
                else if (x[i] < y[i])
                    return -1;
            }
            if (!partial)
            {
                if (x.Length > y.Length)
                    return 1;
                else if (x.Length < y.Length)
                    return -1;
            }
            return 0;
        }

    }
}
