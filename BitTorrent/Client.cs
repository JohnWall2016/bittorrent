using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using BitTorrent.BEncode;
using TorrentFile = BitTorrent.Torrent.File;

namespace BitTorrent.Network
{
    public class FileItem
    {
        public string Path { get; }
        public long Offset { get; }
        public long Length { get; }
        public long EndOffset => Offset + Length;

        public FileItem(string path, long offset, long length)
        {
            this.Path = path;
            this.Offset = offset;
            this.Length = length;
        }

        public void Write(long offset, byte[] bytes, int start, int count)
        {
            using(var stream = new FileStream(
                Path, 
                FileMode.OpenOrCreate,
                FileAccess.Write)
            )
            {
                stream.Seek(offset, SeekOrigin.Begin);
                stream.Write(bytes, start, count);
            }
        }
    }

    public class PiecesState
    {
        const byte NoStart = 0;
        const byte Downloading = 1;
        const byte Corrupted = 2;
        const byte Completed = 3;

        public byte[] Pieces;
        public byte[][] Blockes;

        public PiecesState(int piecesCount, long pieceSize, int blockSize)
        {
            Pieces = new byte[piecesCount];
            Blockes = new byte[piecesCount][];
            for (var i = 0; i < piecesCount; i++)
            {
                var blockCount =  (int)(pieceSize / blockSize) 
                                + (pieceSize % blockSize > 0 ? 1: 0);
                Blockes[i] = new byte[blockCount];
            }
        }

        public void Complete(int piece, int block, Func<int, bool> verifyPiece = null)
        {
            Blockes[piece][block] = Completed;
            if (Blockes[piece].All(x => x == Completed))
            {
                if (verifyPiece != null && !verifyPiece(piece))
                {
                    for (var i = 0; i < Blockes[piece].Length; i++)
                    {
                        Blockes[piece][i] = Corrupted;
                    }
                    Pieces[piece] = Corrupted;
                }
                else
                {
                    Pieces[piece] = Completed;
                }
            }
        }

        public void Save(string path)
        {
            File.WriteAllBytes(path, Encoder.Encode(this));
        }
    }

    public class Client
    {
        public int BlockSize { get; }
        public long PieceSize { get; }
        public long TotalSize { get; }

        public int PiecesCount { get; }

        List<FileItem> _files;

        byte[] _piecesHashes;

        PiecesState _piecesState;

        public Client(TorrentFile file, string saveDir, int port, int blockSize)
        {
            BlockSize = blockSize;
            PieceSize = file.MetaInfo.PieceLength;

            long offset = 0;
            _files = new List<FileItem>();
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

            PiecesCount = (int)(TotalSize / PieceSize) + (TotalSize % PieceSize > 0 ? 1: 0);

            _piecesHashes = file.MetaInfo.Info.Pieces;
            _piecesState = new PiecesState(PiecesCount, PieceSize, BlockSize);
        }

        public async void WritePiece(int piece, int block, byte[] bytes)
        => await Task.Run(() =>
        {
            long start = piece * PieceSize + block * BlockSize;
            int length = bytes.Length;
            long end = start + length;

            foreach (var file in _files)
            {
                if ((start >= file.Offset && start < file.EndOffset) ||
                    (start < file.Offset && end > file.EndOffset) ||
                    (end > file.Offset && end <= file.EndOffset))
                {
                    var fstart = Math.Max(start, file.Offset);
                    var fend = Math.Min(end, file.EndOffset);
                    file.Write(fstart - file.Offset, 
                               bytes, 
                               (int)(fstart - start), 
                               (int)(fend - fstart));

                    if (end <= file.EndOffset) break;
                }
            }

            _piecesState.Complete(piece, block, VerifyPiece);
        }
        );

        bool VerifyPiece(int piece)
        {
            // TODO: Uses _piecesHashes to verfiy the integrity.
            return true;
        }
    }
}