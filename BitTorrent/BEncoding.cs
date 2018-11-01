using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace BitTorrent.BEncoding
{
    public static class Bytes
    {
        public const string _encodeName = "utf8";

        public static byte[] ToBytes(this int i, string encodeName = _encodeName)
        => Encoding.GetEncoding(encodeName).GetBytes(i.ToString());

        public static byte[] ToBytes(this long i, string encodeName = _encodeName)
        => Encoding.GetEncoding(encodeName).GetBytes(i.ToString());

        public static byte[] ToBytes(this string str, string encodeName = _encodeName)
        => Encoding.GetEncoding(encodeName).GetBytes(str);

        public static string ToString(this byte[] bytes, string encodeName = _encodeName)
        => Encoding.GetEncoding(encodeName).GetString(bytes);

        public static byte[] Copy(IEnumerable<byte> src, int count) 
        => src.Take(count).ToArray();

        public static byte[] Join(params byte[][] bytes)
        {
            var length = bytes.Sum(s => s.Length);
            var stream = new MemoryStream(length);
            foreach (var b in bytes)
                stream.Write(b);
            return stream.GetBuffer();
        }
    }

    public interface IDumpable
    {
        byte[] Dump();
    }

    public class String : IDumpable
    {
        byte[] _data = null;

        public String(IEnumerable<byte> src, int count)
        => _data = Bytes.Copy(src, count);

        public byte[] Dump()
        => Bytes.Join(_data.Length.ToBytes(), ":".ToBytes(), _data);
    }

    public class Number : IDumpable
    {
        long _number;

        public Number(string number) => _number = long.Parse(number);

        public byte[] Dump()
        => Bytes.Join("i".ToBytes(), _number.ToBytes(), "e".ToBytes());
    }
}