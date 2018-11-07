using System;
using BitTorrent.BEncode;
using BitTorrent.Torrent;

namespace BitTorrent
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine(new TorrentFile(@"manti.torrent").ToString());
            var torrent = new File(@"manti.torrent");
            Console.WriteLine(torrent.ToString());
        }
    }
}
