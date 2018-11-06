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

    public interface IDumpable
    {
        byte[] Dump();
    }

    public class String : IDumpable
    {
        MemoryStream _data;

        String(MemoryStream data) => _data = data;

        public byte[] Dump()
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

        public static String Parse(IEnumerator<byte> bytes)
        {
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

    public class Number : IDumpable
    {
        public static readonly byte beginToken = "i".ToBytes()[0];
        public static readonly byte endToken = "e".ToBytes()[0];

        MemoryStream _data;

        Number(MemoryStream data) => _data = data;

        public byte[] Dump()
        => Bytes.Join(new byte[]{beginToken}, _data.ToArray(), new byte[]{endToken});

        public long ToLong()
        => long.Parse(Encoding.ASCII.GetString(_data.GetBuffer(), 0, (int)_data.Length));

        public static Number Parse(IEnumerator<byte> bytes)
        {
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

    public class List : IDumpable
    {
        public static readonly byte beginToken = "l".ToBytes()[0];
        public static readonly byte endToken = "e".ToBytes()[0];

        List<IDumpable> _list = new List<IDumpable>();

        public void AddItem(IDumpable item)
        => _list.Add(item);

        public byte[] Dump()
        {
            var stream = new MemoryStream();
            stream.WriteByte(beginToken);
            foreach (var l in _list)
            {
                stream.Write(l.Dump());
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

    public class Dictionary : IDumpable
    {
        public static readonly byte beginToken = "d".ToBytes()[0];
        public static readonly byte endToken = "e".ToBytes()[0];

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
            stream.WriteByte(beginToken);
            foreach (var key in _keys)
            {
                stream.Write(key.Dump());
                stream.Write(_dir[key].Dump());
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

    public class TorrentFile
    {
        Dictionary _dir;

        public TorrentFile(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var enumerable = bytes.AsEnumerable().GetEnumerator();
            IDumpable dumpable = Parse(enumerable, true);
            if (dumpable is Dictionary)
            {
                _dir = dumpable as Dictionary;
            }
            else
            {
                throw new FormatException("The torrent file doesn't "
                    + "include a dictionary structure");
            }
        }

        public IDumpable Parse(IEnumerator<byte> bytes, bool moveNext = true)
        {
            if (moveNext && !bytes.MoveNext())
                throw new FormatException("A empty structure");

            if (bytes.Current == Dictionary.beginToken)
            {
                var dir = new Dictionary();
                while(bytes.MoveNext() && bytes.Current != Dictionary.endToken)
                {
                    var key = Parse(bytes, false);
                    var value = Parse(bytes);
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
                    var item = Parse(bytes, false);
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
                return Number.Parse(bytes);
            }
            else // must be a String
            {
                return String.Parse(bytes);
            }
        }

        public override string ToString() => _dir?.ToString();

        public void Save(string path)
        {
            var bytes = _dir?.Dump();
            File.WriteAllBytes(path, bytes);
        }
    }
}