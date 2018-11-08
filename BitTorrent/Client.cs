using System;
using System.Collections.Generic;
using System.IO;

using TorrentFile = BitTorrent.Torrent.File;

namespace BitTorrent.Network
{
    public class FileItem
    {
        public string Path { get; }
        public long Offset { get; }
        public long Length { get; }

        public FileItem(string path, long offset, long length)
        {
            this.Path = path;
            this.Offset = offset;
            this.Length = length;
        }

        public void Write(long start, byte[] bytes)
        {

        }
    }

    public class Client
    {
        public int BlockSize { get; }
        public long PieceSize { get; }
        public long TotalSize { get; }

        public int PiecesCount { get; }

        public List<FileItem> files;

        byte[] _piecesHashes;
        byte[] _piecesCompleted;
        byte[][] _blockesCompleted;

        public Client(TorrentFile file, string saveDir, int port, int blockSize)
        {
            BlockSize = blockSize;
            PieceSize = file.MetaInfo.PieceLength;

            long offset = 0;
            files = new List<FileItem>();
            foreach (var f in file.MetaInfo.Files)
            {
                var fileItem = new FileItem(
                    Path.Join(saveDir, f.Path),
                    offset,
                    f.Length
                );
                offset += f.Length;
            }
            TotalSize = offset;

            int getCount(long total, long section)
            => (int)(total / section) + (total % section > 0 ? 1: 0);

            PiecesCount = getCount(TotalSize, PieceSize);

            _piecesHashes = file.MetaInfo.Info.Pieces;
            _piecesCompleted = new byte[PiecesCount];
            for (var i = 0; i < PiecesCount; i++)
            {
                _blockesCompleted[i] = new byte[getCount(PieceSize, blockSize)];
            }
        }

        public void WritePiece(int piece, int block, byte[] bytes)
        {
            
        }
    }
}