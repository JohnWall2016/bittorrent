using System;
using Xunit;
using Xunit.Abstractions;
using BitTorrent.BEncoding;

namespace BitTorrentTest
{
    public class BEncodingTest
    {
        private readonly ITestOutputHelper output;

        public BEncodingTest(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void TestBytes()
        {
            var bytes = 12345.ToBytes();
            output.WriteLine("{0}:[{1}]", bytes.Length, bytes.Dump());
            bytes = "i".ToBytes();
            output.WriteLine("{0}:[{1}]", bytes.Length, bytes.Dump());
        }

        [Fact]
        public void TestTorrentFile()
        {
            var torrent = new TorrentFile(@"..\..\..\manti.torrent");
            output.WriteLine(torrent.ToString());
            torrent.Save(@"..\..\..\manti_dump.torrent");
        }
    }
}
