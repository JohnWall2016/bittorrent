using System;
using System.IO;
using System.Linq;

using IOFile = System.IO.File;

using BitTorrent.BEncode;
using BitTorrent.Utils;

namespace BitTorrent.Torrent
{
    public class File
    {
        IEncodable _encodable;

        public MetaInfo MetaInfo { get; }

        public File(string path)
        {
            var bytes = IOFile.ReadAllBytes(path);
            _encodable = Decoder.Parse(bytes);
            MetaInfo = Decoder.Decode<MetaInfo>(_encodable);
        }

        public override string ToString() => MetaInfo?.ToString();

        public void Save(string path)
        {
            var bytes = _encodable?.Encode();
            IOFile.WriteAllBytes(path, bytes);
        }
    }

    public class FileInfo
    {
        [BEncode("length")]
        public long Length;

        [BEncode("path")]
        public string Path;

        public override string ToString()
        {
            return $"{{\"Path\":{Path}, \"Length\":{Length}}}";
        }
    }

    public class Info
    {
        [BEncode("length")]
        public long? Length;

        [BEncode("name")]
        public string Name;

        [BEncode("piece length")]
        public long PieceLength;

        [BEncode("pieces")]
        public byte[] Pieces;

        [BEncode("private")]
        public int? Private;

        [BEncode("files")]
        public FileInfo[] Files;

        public override string ToString()
        {
            return "{" +
                $"\"Length\":{Length}, " +
                $"\"Name\":{Name}, " +
                $"\"PieceLength\":{PieceLength}, " +
                $"\"Pieces\":[{Bytes.Dump(Pieces, 50)}], " +
                $"\"Private\":{Private}, " +
                $"\"Files\":[{Files?.Join()}]" +
                "}";
        }
    }

    public class MetaInfo
    {
        [BEncode("announce")]
        public string Announce;

        [BEncode("announce-list")]
        public string[] AnnounceList;

        [BEncode("comment")]
        public string Comment;

        [BEncode("created by")]
        public string CreatedBy;

        [BEncode("creation date")]
        public long? CreatedDate;

        [BEncode("encoding")]
        public string Encoding;

        [BEncode("info")]
        public Info Info;

        public override string ToString()
        {
            return "{" +
                $"\"Announce\":{Announce}, " +
                $"\"AnnounceList\":[{AnnounceList?.Join()}], " +
                $"\"Coumment\":{Comment}, " +
                $"\"CreatedBy\":{CreatedBy}, " +
                $"\"CreatedDate\":{CreatedDate}, " +
                $"\"Encoding\":{Encoding}, " +
                $"\"Info\":{Info}" +
                "}";
        }
    }
}