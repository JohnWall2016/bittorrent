using System.IO;
using System.Linq;
using System.Text;

namespace BitTorrent.Utils
{
    public static class Bytes
    {
        public const string EncodeName = "utf-8";

        public static byte[] ToBytes(this int i, string encodeName = EncodeName)
        => Encoding.GetEncoding(encodeName).GetBytes(i.ToString());

        public static byte[] ToBytes(this long i, string encodeName = EncodeName)
        => Encoding.GetEncoding(encodeName).GetBytes(i.ToString());

        public static byte[] ToBytes(this string str, string encodeName = EncodeName)
        => Encoding.GetEncoding(encodeName).GetBytes(str);

        public static string GetString(this byte[] bytes, string encodeName = EncodeName)
        => Encoding.GetEncoding(encodeName).GetString(bytes);

        public static string GetString(this byte[] bytes, int index, int count, 
            string encodeName = EncodeName)
        => Encoding.GetEncoding(encodeName).GetString(bytes, index, count);

        public static string Dump(this byte[] bytes, int length = -1)
        {
            var builder = new StringBuilder();
            length = (length < 0) ? bytes.Length 
                : (length > bytes.Length ? bytes.Length : length);
            for (var i = 0; i < length; i++)
            {
                if (i > 0) builder.Append(" ");
                builder.Append(bytes[i].ToString("X"));
            }
            if (length < bytes.Length) builder.Append(" ...");
            return builder.ToString();
        }

        public static byte[] Join(params byte[][] bytes)
        {
            var length = bytes.Sum(s => s.Length);
            var stream = new MemoryStream(length);
            foreach (var b in bytes)
            {
                stream.Write(b);
            }
            return stream.ToArray();
        }
    }

    public static class Arrays
    {
        public static string Join<T>(this T[] a, string separator = ", ")
        {
            var builder = new StringBuilder();
            for (var i = 0; i < a.Length; i++)
            {
                if (i > 0) builder.Append(separator);
                builder.Append(a[i].ToString());
            }
            return builder.ToString();
        }
    }
}