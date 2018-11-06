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

        public static string ToString(this byte[] bytes, int index, int count, 
            string encodeName = EncodeName)
        => Encoding.GetEncoding(encodeName).GetString(bytes, index, count);

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
            {
                stream.Write(b);
            }
            return stream.ToArray();
        }
    }

    public interface IEncodable
    {
        byte[] Encode();
    }

    public class String : IEncodable
    {
        MemoryStream _data;

        String(MemoryStream data) => _data = data;

        public byte[] Encode()
        => Bytes.Join(_data.Length.ToBytes(), ":".ToBytes(), _data.ToArray());

        public string ToString(string encodeName = Bytes.EncodeName, int length = -1)
        {
            if (length == -1)
            {
                return Encoding.GetEncoding(encodeName)
                    .GetString(_data.GetBuffer(), 0, (int)_data.Length);
            }
            else
            {
                length = length < _data.Length ? length : (int)_data.Length;
                return Encoding.GetEncoding(encodeName)
                    .GetString(_data.GetBuffer(), 0, length);
            }
        }

        public static String Decode(IEnumerator<byte> bytes, bool moveNext = true)
        {
            if (moveNext && !bytes.MoveNext())
                throw new FormatException("A empty String structure");

            var slen = new MemoryStream();
            while (bytes.Current >= 0x30 && bytes.Current <= 0x39) // 0-9
            {
                slen.WriteByte(bytes.Current);
                if (!bytes.MoveNext())
                {
                    throw new FormatException("String length is invalid");
                }
            }
            if (bytes.Current != 0x3a) // :
            {
                throw new FormatException("String mark ':' is not found");
            }
            int len = int.Parse(Bytes.ToString(slen.GetBuffer(), 0, (int)slen.Length));
            var data = new MemoryStream(len);
            while (len > 0)
            {
                if (!bytes.MoveNext())
                {    
                    throw new FormatException($"String data is shorter than length: {len}");
                }
                data.WriteByte(bytes.Current);
                len -= 1;
            }
            return new String(data);
        }

        public override string ToString() => $"\"{ToString(length: 150)}\"";
    }

    public class Number : IEncodable
    {
        public static readonly byte beginToken = "i".ToBytes()[0];
        public static readonly byte endToken = "e".ToBytes()[0];

        MemoryStream _data;

        Number(MemoryStream data) => _data = data;

        public byte[] Encode()
        => Bytes.Join(new byte[]{beginToken}, _data.ToArray(), new byte[]{endToken});

        public long ToLong()
        => long.Parse(Encoding.ASCII.GetString(_data.GetBuffer(), 0, (int)_data.Length));

        public static Number Decode(IEnumerator<byte> bytes, bool moveNext = true)
        {
            if (moveNext && !bytes.MoveNext())
                throw new FormatException("A empty structure");

            if (bytes.Current != beginToken)
            {    
                throw new FormatException("Number beginToken is not found");
            }
            var data = new MemoryStream();
            while (bytes.MoveNext() && bytes.Current >= 0x30 && bytes.Current <= 0x39) // 0-9
            {
                data.WriteByte(bytes.Current);
            }
            if (bytes.Current != endToken)
            {    
                throw new FormatException("Number endToken is not found");
            }
            if (data.Length <= 0)
            {
                throw new FormatException("Number length is shorter than 1");
            }
            return new Number(data);
        }

        public override string ToString() => $"{ToLong()}";
    }

    public class List : IEncodable
    {
        public static readonly byte beginToken = "l".ToBytes()[0];
        public static readonly byte endToken = "e".ToBytes()[0];

        List<IEncodable> _list = new List<IEncodable>();

        public void AddItem(IEncodable item)
        => _list.Add(item);

        public byte[] Encode()
        {
            var stream = new MemoryStream();
            stream.WriteByte(beginToken);
            foreach (var l in _list)
            {
                stream.Write(l.Encode());
            }
            stream.WriteByte(endToken);
            return stream.ToArray();
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            str.Append("[");
            for (var i = 0; i < _list.Count; i++)
            {
                if (i > 0) str.Append(", ");
                str.Append(_list[i].ToString());
            }
            str.Append("]");
            return str.ToString();
        }
    }

    public class Dictionary : IEncodable
    {
        public static readonly byte beginToken = "d".ToBytes()[0];
        public static readonly byte endToken = "e".ToBytes()[0];

        List<IEncodable> _keys = new List<IEncodable>();
        Dictionary<IEncodable, IEncodable> _dir = new Dictionary<IEncodable, IEncodable>();

        public void AddPair(IEncodable key, IEncodable value)
        {
            if (_keys.Contains(key)) return;
            _keys.Add(key);
            _dir[key] = value;
        }

        public byte[] Encode()
        {
            var stream = new MemoryStream();
            stream.WriteByte(beginToken);
            foreach (var key in _keys)
            {
                stream.Write(key.Encode());
                stream.Write(_dir[key].Encode());
            }
            stream.WriteByte(endToken);
            return stream.ToArray();
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            str.Append("{");
            for (var i = 0; i < _keys.Count; i++)
            {
                if (i > 0) str.Append(", ");
                var key = _keys[i];
                str.Append(key.ToString());
                str.Append(": ");
                str.Append(_dir[key].ToString());
            }
            str.Append("}");
            return str.ToString();
        }
    }

    public static class BEncoding
    {
        public static T Decode<T>(byte[] bytes) where T: class, IEncodable
        {
            return Decode(bytes.AsEnumerable().GetEnumerator()) as T;
        }

        static IEncodable Decode(IEnumerator<byte> bytes, bool moveNext = true)
        {
            if (moveNext && !bytes.MoveNext())
                throw new FormatException("A empty structure");

            if (bytes.Current == Dictionary.beginToken)
            {
                var dir = new Dictionary();
                while(bytes.MoveNext() && bytes.Current != Dictionary.endToken)
                {
                    var key = Decode(bytes, false);
                    var value = Decode(bytes);
                    dir.AddPair(key, value);
                }
                if (bytes.Current != Dictionary.endToken)
                {    
                    throw new FormatException("Dictionary endToken is not found");
                }
                return dir;
            }
            else if (bytes.Current == List.beginToken)
            {
                var list = new List();
                while(bytes.MoveNext() && bytes.Current != List.endToken)
                {
                    var item = Decode(bytes, false);
                    list.AddItem(item);
                }
                if (bytes.Current != List.endToken)
                {    
                    throw new FormatException("List endToken is not found");
                }
                return list;
            }
            else if (bytes.Current == Number.beginToken)
            {
                return Number.Decode(bytes, false);
            }
            else // must be a String
            {
                return String.Decode(bytes, false);
            }
        }
    }

    public class TorrentFile
    {
        Dictionary _dir;

        public TorrentFile(string path)
        {
            var bytes = File.ReadAllBytes(path);
            _dir = BEncoding.Decode<Dictionary>(bytes);
        }

        public override string ToString() => _dir?.ToString();

        public void Save(string path)
        {
            var bytes = _dir?.Encode();
            File.WriteAllBytes(path, bytes);
        }
    }
}