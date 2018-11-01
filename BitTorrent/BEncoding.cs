using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace BitTorrent.BEncoding
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

        public static string ToString(this byte[] bytes, string encodeName = EncodeName)
        => Encoding.GetEncoding(encodeName).GetString(bytes);

        public static string Dump(this byte[] bytes)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < bytes.Length; i++)
            {
                if (i > 0) builder.Append(" ");
                builder.Append(bytes[i].ToString("X"));
            }
            return builder.ToString();
        }

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
        MemoryStream _data = new MemoryStream();

        public void AddString(byte[] buffer, int offset, int count)
        => _data.Write(buffer, offset, count);

        public byte[] Dump()
        => Bytes.Join(_data.Length.ToBytes(), ":".ToBytes(), _data.GetBuffer());

        public string ToString(string encodeName = Bytes.EncodeName)
        => Encoding.GetEncoding(encodeName).GetString(_data.GetBuffer());
    }

    public class Number : IDumpable
    {
        MemoryStream _data = new MemoryStream();

        public void AddDigit(byte digit)
        => _data.WriteByte(digit);

        public byte[] Dump()
        => Bytes.Join("i".ToBytes(), _data.GetBuffer(), "e".ToBytes());

        public long ToLong()
        => long.Parse(Encoding.ASCII.GetString(_data.GetBuffer()));
    }

    public class List : IDumpable
    {
        List<IDumpable> _list = new List<IDumpable>();

        public void AddItem(IDumpable item)
        => _list.Add(item);

        public byte[] Dump()
        {
            var stream = new MemoryStream();
            stream.Write("l".ToBytes());
            foreach (var l in _list)
            {
                stream.Write(l.Dump());
            }
            stream.Write("e".ToBytes());
            return stream.GetBuffer();
        }
    }

    public class Dictionary : IDumpable
    {
        List<IDumpable> _keys = new List<IDumpable>();
        Dictionary<IDumpable, IDumpable> _dir = new Dictionary<IDumpable, IDumpable>();

        public void AddPair(IDumpable key, IDumpable value)
        {
            if (_keys.Contains(key)) return;
            _keys.Add(key);
            _dir[key] = value;
        }

        public byte[] Dump()
        {
            var stream = new MemoryStream();
            stream.Write("d".ToBytes());
            foreach (var key in _keys)
            {
                stream.Write(key.Dump());
                stream.Write(_dir[key].Dump());
            }
            stream.Write("e".ToBytes());
            return stream.GetBuffer();
        }
    }

    public class TorrentFile
    {
        
    }
}