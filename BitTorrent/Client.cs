using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        enum State { NoStart = 0, Downloading, Completed, Corrupted }

        State[] _pieces;
        State[][] _blockes;

        public PiecesState(int piecesCount, long pieceSize, int blockSize)
        {
            _pieces = new State[piecesCount];
            _blockes = new State[piecesCount][];
            for (var i = 0; i < piecesCount; i++)
            {
                var blockCount =  (int)(pieceSize / blockSize) 
                                + (pieceSize % blockSize > 0 ? 1: 0);
                _blockes[i] = new State[blockCount];
            }
        }

        public void Complete(int piece, int block, Func<int, bool> verifyPiece = null)
        {
            _blockes[piece][block] = State.Completed;
            if (_blockes[piece].All(x => x == State.Completed))
            {
                if (verifyPiece != null && !verifyPiece(piece))
                {
                    for (var i = 0; i < _blockes[piece].Length; i++)
                    {
                        _blockes[piece][i] = State.Corrupted;
                    }
                    _pieces[piece] = State.Corrupted;
                }
                else
                {
                    _pieces[piece] = State.Completed;
                }
            }
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