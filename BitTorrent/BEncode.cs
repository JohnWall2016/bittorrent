using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace BitTorrent.BEncode
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

    public interface IEncodable
    {
        byte[] Encode();
    }

    public class ByteArray : IEncodable
    {
        byte[] _data;

        public byte[] Data => _data;

        ByteArray(byte[] data) => _data = data;

        public byte[] Encode() 
        => Bytes.Join(_data.Length.ToBytes(), ":".ToBytes(), _data);

        public static byte[] Encode(string s)
        => Bytes.Join(s.Length.ToBytes(), ":".ToBytes(), s.ToBytes());

        public string AsString(string encodeName = Bytes.EncodeName, 
                               int length = -1)
        {
            if (length == -1)
            {
                return Encoding.GetEncoding(encodeName).GetString(_data);
            }
            else
            {
                length = length < _data.Length ? length : _data.Length;
                return Encoding.GetEncoding(encodeName)
                    .GetString(_data, 0, length);
            }
        }

        public static ByteArray Decode(IEnumerator<byte> bytes, 
                                    bool moveNext = true)
        {
            if (moveNext && !bytes.MoveNext())
                throw new FormatException("A empty ByteArray structure");

            var slen = new MemoryStream();
            while (bytes.Current >= 0x30 && bytes.Current <= 0x39) // 0-9
            {
                slen.WriteByte(bytes.Current);
                if (!bytes.MoveNext())
                {
                    throw new FormatException("ByteArray length is invalid");
                }
            }
            if (bytes.Current != 0x3a) // :
            {
                throw new FormatException("ByteArray data mark ':' is not found");
            }
            int len = int.Parse(Bytes.ToString(slen.GetBuffer(), 0, (int)slen.Length));
            var data = new MemoryStream(len);
            while (len > 0)
            {
                if (!bytes.MoveNext())
                {    
                    throw new FormatException($"ByteArray data is shorter than length: {len}");
                }
                data.WriteByte(bytes.Current);
                len -= 1;
            }
            return new ByteArray(data.ToArray());
        }

        public override string ToString() => $"\"{AsString(length: 150)}\"";
    }

    public class Number : IEncodable
    {
        public static readonly byte beginToken = "i".ToBytes()[0];
        public static readonly byte endToken = "e".ToBytes()[0];

        byte[] _data;

        Number(byte[] data) => _data = data;

        public byte[] Encode()
        => Bytes.Join(new byte[]{beginToken}, _data, new byte[]{endToken});

        public static byte[] Encode(long l)
        => Bytes.Join(new byte[]{beginToken}, l.ToBytes(), new byte[]{endToken});

        public long AsLong()
        => long.Parse(Encoding.ASCII.GetString(_data));

        public static Number Decode(IEnumerator<byte> bytes, 
                                    bool moveNext = true)
        {
            if (moveNext && !bytes.MoveNext())
                throw new FormatException("A empty Number structure");

            if (bytes.Current != beginToken)
            {    
                throw new FormatException("Number beginToken is not found");
            }
            var data = new MemoryStream();
            while (bytes.MoveNext() 
                && bytes.Current >= 0x30 && bytes.Current <= 0x39) // 0-9
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
            return new Number(data.ToArray());
        }

        public override string ToString() => $"{AsLong()}";
    }

    public class List : IEncodable
    {
        public static readonly byte beginToken = "l".ToBytes()[0];
        public static readonly byte endToken = "e".ToBytes()[0];

        List<IEncodable> _list = new List<IEncodable>();

        public void AddItem(IEncodable item) => _list.Add(item);

        public int Count => _list.Count;

        public IEncodable this[int i] => _list[i];

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
        Dictionary<IEncodable, IEncodable> _dir =
            new Dictionary<IEncodable, IEncodable>();

        public void AddPair(IEncodable key, IEncodable value)
        {
            if (_keys.Contains(key)) return;
            _keys.Add(key);
            _dir[key] = value;
        }

        public IEncodable this[string key]
        {
            get 
            {
                foreach (var k in _keys)
                {
                    if (k is ByteArray)
                    {
                        if ((k as ByteArray).AsString() == key)
                            return _dir[k];
                    }
                }
                return null;
            }
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

    public sealed class BEncodeAttribute : Attribute
    {
        public BEncodeAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }

    public static class Decoder
    {
        static object Decode(Type type, IEncodable encodable)
        {
            if (type == typeof(int)
                || type == typeof(long)
                || type == typeof(Nullable<int>) 
                || type == typeof(Nullable<long>))
            {
                if (encodable is Number)
                {
                    if (type == typeof(int))
                    {
                        return (int)(encodable as Number).AsLong();
                    }
                    else
                    {
                        return (encodable as Number).AsLong();
                    }
                }
            }
            else if (type == typeof(string))
            {
                if (encodable is ByteArray)
                {
                    return (encodable as ByteArray).AsString();
                }
                else if (encodable is List)
                {
                    var list = encodable as List;
                    if (list.Count == 1 && list[0] is ByteArray)
                    {
                        return (list[0] as ByteArray).AsString();
                    }
                }
            }
            else if (type.IsArray)
            {
                if (encodable is List)
                {
                    var list = encodable as List;
                    var elemType = type.GetElementType();
                    var array = Array.CreateInstance(elemType, list.Count);
                    for (var i = 0; i < list.Count; i++)
                    {
                        array.SetValue(Decode(elemType, list[i]), i);
                    }
                    return array;
                }
                else if (type.GetElementType() == typeof(byte) && encodable is ByteArray)
                {
                    return (encodable as ByteArray).Data;
                }
            }
            else
            {
                if (encodable is Dictionary)
                {
                    var dir = encodable as Dictionary;
                    var obj = Activator.CreateInstance(type);
                    var fields = type.GetFields();
                    foreach (var f in fields)
                    {
                        var name = f.Name;
                        var attr = (BEncodeAttribute)Attribute
                            .GetCustomAttribute(f, typeof(BEncodeAttribute));
                        if (attr != null) name = attr.Name;

                        var t = f.FieldType;
                        var v = dir[name];

                        f.SetValue(obj, Decode(t, v));
                    }
                    return obj;
                }
            }
            return null;
        }

        public static T Decode<T>(byte[] bytes)
        => (T)Decode(typeof(T), Parse(bytes));

        public static T Decode<T>(IEncodable encodable)
        => (T)Decode(typeof(T), encodable);

        static IEncodable Parse(IEnumerator<byte> bytes, bool moveNext = true)
        {
            if (moveNext && !bytes.MoveNext())
                throw new FormatException("A empty IEncodable structure");

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
                return Number.Decode(bytes, false);
            }
            else // must be a byte array
            {
                return ByteArray.Decode(bytes, false);
            }
        }

        public static IEncodable Parse(byte[] bytes)
        => Parse(bytes.AsEnumerable().GetEnumerator());
    }
}