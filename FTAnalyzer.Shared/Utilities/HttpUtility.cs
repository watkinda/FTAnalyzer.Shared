﻿using System.Text;
using System.Net;
using System.Windows;
using System.Diagnostics;

namespace System.Web
{
    public sealed class HttpUtility
    {
        private static int HexToInt(char h)
        {
            if ((h >= '0') && (h <= '9'))
            {
                return (h - '0');
            }
            if ((h >= 'a') && (h <= 'f'))
            {
                return ((h - 'a') + 10);
            }
            if ((h >= 'A') && (h <= 'F'))
            {
                return ((h - 'A') + 10);
            }
            return -1;
        }

        internal static char IntToHex(int n)
        {
            if (n <= 9)
            {
                return (char)(n + 0x30);
            }
            return (char)((n - 10) + 0x61);
        }

        private static bool IsNonAsciiByte(byte b)
        {
            if (b < 0x7f)
            {
                return (b < 0x20);
            }
            return true;
        }

        internal static bool IsSafe(char ch)
        {
            if ((((ch >= 'a') && (ch <= 'z')) || ((ch >= 'A') && (ch <= 'Z'))) || ((ch >= '0') && (ch <= '9')))
            {
                return true;
            }
            switch (ch)
            {
                case '\'':
                case '(':
                case ')':
                case '*':
                case '-':
                case '.':
                case '_':
                case '!':
                    return true;
            }
            return false;
        }

        public static string UrlDecode(string str)
        {
            if (str == null)
            {
                return null;
            }
            return UrlDecode(str, Encoding.UTF8);
        }

        public static string UrlDecode(byte[] bytes, Encoding e)
        {
            if (bytes == null)
            {
                return null;
            }
            return UrlDecode(bytes, 0, bytes.Length, e);
        }

        public static string UrlDecode(string str, Encoding e)
        {
            if (str == null)
            {
                return null;
            }
            return UrlDecodeStringFromStringInternal(str, e);
        }

        public static string UrlDecode(byte[] bytes, int offset, int count, Encoding e)
        {
            if ((bytes == null) && (count == 0))
            {
                return null;
            }
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if ((offset < 0) || (offset > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((count < 0) || ((offset + count) > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return UrlDecodeStringFromBytesInternal(bytes, offset, count, e);
        }

        private static byte[] UrlDecodeBytesFromBytesInternal(byte[] buf, int offset, int count)
        {
            int length = 0;
            byte[] sourceArray = new byte[count];
            for (int i = 0; i < count; i++)
            {
                int index = offset + i;
                byte num4 = buf[index];
                if (num4 == 0x2b)
                {
                    num4 = 0x20;
                }
                else if ((num4 == 0x25) && (i < (count - 2)))
                {
                    int num5 = HexToInt((char)buf[index + 1]);
                    int num6 = HexToInt((char)buf[index + 2]);
                    if ((num5 >= 0) && (num6 >= 0))
                    {
                        num4 = (byte)((num5 << 4) | num6);
                        i += 2;
                    }
                }
                sourceArray[length++] = num4;
            }
            if (length < sourceArray.Length)
            {
                byte[] destinationArray = new byte[length];
                Array.Copy(sourceArray, destinationArray, length);
                sourceArray = destinationArray;
            }
            return sourceArray;
        }

        private static string UrlDecodeStringFromBytesInternal(byte[] buf, int offset, int count, Encoding e)
        {
            UrlDecoder decoder = new UrlDecoder(count, e);
            for (int i = 0; i < count; i++)
            {
                int index = offset + i;
                byte b = buf[index];
                if (b == 0x2b)
                {
                    b = 0x20;
                }
                else if ((b == 0x25) && (i < (count - 2)))
                {
                    if ((buf[index + 1] == 0x75) && (i < (count - 5)))
                    {
                        int num4 = HexToInt((char)buf[index + 2]);
                        int num5 = HexToInt((char)buf[index + 3]);
                        int num6 = HexToInt((char)buf[index + 4]);
                        int num7 = HexToInt((char)buf[index + 5]);
                        if (((num4 < 0) || (num5 < 0)) || ((num6 < 0) || (num7 < 0)))
                        {
                            goto Label_00DA;
                        }
                        char ch = (char)((((num4 << 12) | (num5 << 8)) | (num6 << 4)) | num7);
                        i += 5;
                        decoder.AddChar(ch);
                        continue;
                    }
                    int num8 = HexToInt((char)buf[index + 1]);
                    int num9 = HexToInt((char)buf[index + 2]);
                    if ((num8 >= 0) && (num9 >= 0))
                    {
                        b = (byte)((num8 << 4) | num9);
                        i += 2;
                    }
                }
            Label_00DA:
                decoder.AddByte(b);
            }
            return decoder.GetString();
        }

        private static string UrlDecodeStringFromStringInternal(string s, Encoding e)
        {
            int length = s.Length;
            UrlDecoder decoder = new UrlDecoder(length, e);
            for (int i = 0; i < length; i++)
            {
                char ch = s[i];
                if (ch == '+')
                {
                    ch = ' ';
                }
                else if ((ch == '%') && (i < (length - 2)))
                {
                    if ((s[i + 1] == 'u') && (i < (length - 5)))
                    {
                        int num3 = HexToInt(s[i + 2]);
                        int num4 = HexToInt(s[i + 3]);
                        int num5 = HexToInt(s[i + 4]);
                        int num6 = HexToInt(s[i + 5]);
                        if (((num3 < 0) || (num4 < 0)) || ((num5 < 0) || (num6 < 0)))
                        {
                            goto Label_0106;
                        }
                        ch = (char)((((num3 << 12) | (num4 << 8)) | (num5 << 4)) | num6);
                        i += 5;
                        decoder.AddChar(ch);
                        continue;
                    }
                    int num7 = HexToInt(s[i + 1]);
                    int num8 = HexToInt(s[i + 2]);
                    if ((num7 >= 0) && (num8 >= 0))
                    {
                        byte b = (byte)((num7 << 4) | num8);
                        i += 2;
                        decoder.AddByte(b);
                        continue;
                    }
                }
            Label_0106:
                if ((ch & 0xff80) == 0)
                {
                    decoder.AddByte((byte)ch);
                }
                else
                {
                    decoder.AddChar(ch);
                }
            }
            return decoder.GetString();
        }

        public static byte[] UrlDecodeToBytes(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            return UrlDecodeToBytes(bytes, 0, (bytes != null) ? bytes.Length : 0);
        }

        public static byte[] UrlDecodeToBytes(string str)
        {
            if (str == null)
            {
                return null;
            }
            return UrlDecodeToBytes(str, Encoding.UTF8);
        }

        public static byte[] UrlDecodeToBytes(string str, Encoding e)
        {
            if (str == null)
            {
                return null;
            }
            return UrlDecodeToBytes(e.GetBytes(str));
        }

        public static byte[] UrlDecodeToBytes(byte[] bytes, int offset, int count)
        {
            if ((bytes == null) && (count == 0))
            {
                return null;
            }
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if ((offset < 0) || (offset > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((count < 0) || ((offset + count) > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return UrlDecodeBytesFromBytesInternal(bytes, offset, count);
        }

        public static string UrlEncode(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes));
        }

        public static string UrlEncode(string str)
        {
            if (str == null)
            {
                return null;
            }
            return UrlEncode(str, Encoding.UTF8);
        }

        public static string UrlEncode(string str, Encoding e)
        {
            if (str == null)
            {
                return null;
            }
            return Encoding.ASCII.GetString(UrlEncodeToBytes(str, e));
        }

        public static string UrlEncode(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
            {
                return null;
            }
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes, offset, count));
        }

        private static byte[] UrlEncodeBytesToBytesInternal(byte[] bytes, int offset, int count,
                                                            bool alwaysCreateReturnValue)
        {
            int num = 0;
            int num2 = 0;
            for (int i = 0; i < count; i++)
            {
                char ch = (char)bytes[offset + i];
                if (ch == ' ')
                {
                    num++;
                }
                else if (!IsSafe(ch))
                {
                    num2++;
                }
            }
            if ((!alwaysCreateReturnValue && (num == 0)) && (num2 == 0))
            {
                return bytes;
            }
            byte[] buffer = new byte[count + (num2 * 2)];
            int num4 = 0;
            for (int j = 0; j < count; j++)
            {
                byte num6 = bytes[offset + j];
                char ch2 = (char)num6;
                if (IsSafe(ch2))
                {
                    buffer[num4++] = num6;
                }
                else if (ch2 == ' ')
                {
                    buffer[num4++] = 0x2b;
                }
                else
                {
                    buffer[num4++] = 0x25;
                    buffer[num4++] = (byte)IntToHex((num6 >> 4) & 15);
                    buffer[num4++] = (byte)IntToHex(num6 & 15);
                }
            }
            return buffer;
        }

        private static byte[] UrlEncodeBytesToBytesInternalNonAscii(byte[] bytes, int offset, int count,
                                                                    bool alwaysCreateReturnValue)
        {
            int num = 0;
            for (int i = 0; i < count; i++)
            {
                if (IsNonAsciiByte(bytes[offset + i]))
                {
                    num++;
                }
            }
            if (!alwaysCreateReturnValue && (num == 0))
            {
                return bytes;
            }
            byte[] buffer = new byte[count + (num * 2)];
            int num3 = 0;
            for (int j = 0; j < count; j++)
            {
                byte b = bytes[offset + j];
                if (IsNonAsciiByte(b))
                {
                    buffer[num3++] = 0x25;
                    buffer[num3++] = (byte)IntToHex((b >> 4) & 15);
                    buffer[num3++] = (byte)IntToHex(b & 15);
                }
                else
                {
                    buffer[num3++] = b;
                }
            }
            return buffer;
        }

        internal static string UrlEncodeNonAscii(string str, Encoding e)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            if (e == null)
            {
                e = Encoding.UTF8;
            }
            byte[] bytes = e.GetBytes(str);
            bytes = UrlEncodeBytesToBytesInternalNonAscii(bytes, 0, bytes.Length, false);
            return Encoding.ASCII.GetString(bytes);
        }

        internal static string UrlEncodeSpaces(string str)
        {
            if ((str != null) && (str.IndexOf(' ') >= 0))
            {
                str = str.Replace(" ", "%20");
            }
            return str;
        }

        public static byte[] UrlEncodeToBytes(string str)
        {
            if (str == null)
            {
                return null;
            }
            return UrlEncodeToBytes(str, Encoding.UTF8);
        }

        public static byte[] UrlEncodeToBytes(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            return UrlEncodeToBytes(bytes, 0, bytes.Length);
        }

        public static byte[] UrlEncodeToBytes(string str, Encoding e)
        {
            if (str == null)
            {
                return null;
            }
            byte[] bytes = e.GetBytes(str);
            return UrlEncodeBytesToBytesInternal(bytes, 0, bytes.Length, false);
        }

        public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
        {
            if ((bytes == null) && (count == 0))
            {
                return null;
            }
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if ((offset < 0) || (offset > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((count < 0) || ((offset + count) > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return UrlEncodeBytesToBytesInternal(bytes, offset, count, true);
        }

        public static string UrlEncodeUnicode(string str)
        {
            if (str == null)
            {
                return null;
            }
            return UrlEncodeUnicodeStringToStringInternal(str, false);
        }

        private static string UrlEncodeUnicodeStringToStringInternal(string s, bool ignoreAscii)
        {
            int length = s.Length;
            StringBuilder builder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                char ch = s[i];
                if ((ch & 0xff80) == 0)
                {
                    if (ignoreAscii || IsSafe(ch))
                    {
                        builder.Append(ch);
                    }
                    else if (ch == ' ')
                    {
                        builder.Append('+');
                    }
                    else
                    {
                        builder.Append('%');
                        builder.Append(IntToHex((ch >> 4) & '\x000f'));
                        builder.Append(IntToHex(ch & '\x000f'));
                    }
                }
                else
                {
                    builder.Append("%u");
                    builder.Append(IntToHex((ch >> 12) & '\x000f'));
                    builder.Append(IntToHex((ch >> 8) & '\x000f'));
                    builder.Append(IntToHex((ch >> 4) & '\x000f'));
                    builder.Append(IntToHex(ch & '\x000f'));
                }
            }
            return builder.ToString();
        }

        public static byte[] UrlEncodeUnicodeToBytes(string str)
        {
            if (str == null)
            {
                return null;
            }
            return Encoding.ASCII.GetBytes(UrlEncodeUnicode(str));
        }

        public static string UrlPathEncode(string str)
        {
            if (str == null)
            {
                return null;
            }
            int index = str.IndexOf('?');
            if (index >= 0)
            {
                return (UrlPathEncode(str.Substring(0, index)) + str.Substring(index));
            }
            return UrlEncodeSpaces(UrlEncodeNonAscii(str, Encoding.UTF8));
        }

        public static void SetDefaultProxy()
        {
            HttpWebRequest request = HttpWebRequest.Create(new Uri("http://www.google.com")) as HttpWebRequest;
            IWebProxy proxy = request.Proxy;
            if (proxy != null)
            {
                string proxyuri = proxy.GetProxy(request.RequestUri).ToString();
                request.UseDefaultCredentials = true;
                proxy = new WebProxy(proxyuri, false)
                {
                    Credentials = CredentialCache.DefaultCredentials
                };
                WebRequest.DefaultWebProxy = proxy;
            }
        }

        public static void VisitWebsite(string url)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo(url);
                Process.Start(url);
            }
            catch(Exception e)
            {
                MessageBox.Show("Error processing web request. Error was : " + e.Message + "\nSite was: " + url, "FTAnalyzer");
            }
        }

        // Nested Types
        private class UrlDecoder
        {
            // Fields
            private int _bufferSize;
            private byte[] _byteBuffer;
            private char[] _charBuffer;
            private Encoding _encoding;
            private int _numBytes;
            private int _numChars;

            // Methods
            internal UrlDecoder(int bufferSize, Encoding encoding)
            {
                _bufferSize = bufferSize;
                _encoding = encoding;
                _charBuffer = new char[bufferSize];
            }

            internal void AddByte(byte b)
            {
                if (_byteBuffer == null)
                {
                    _byteBuffer = new byte[_bufferSize];
                }
                _byteBuffer[_numBytes++] = b;
            }

            internal void AddChar(char ch)
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }
                _charBuffer[_numChars++] = ch;
            }

            private void FlushBytes()
            {
                if (_numBytes > 0)
                {
                    _numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
                    _numBytes = 0;
                }
            }

            internal string GetString()
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }
                if (_numChars > 0)
                {
                    return new string(_charBuffer, 0, _numChars);
                }
                return string.Empty;
            }
        }
    }
}
