using System;
using Xunit;
using Xunit.Abstractions;

using BitTorrent.BEncode;
using BitTorrent.Torrent;

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
            var torrent = new File(@"../../../manti.torrent");
            output.WriteLine(torrent.ToString());
            // torrent.Save(@"..\..\..\manti_dump.torrent");
            output.WriteLine(torrent.MetaInfo.AnnounceList[0]);
            output.WriteLine(torrent.MetaInfo.Info.Name);
        }

        [Fact]
        public void TestDecode()
        {
            var s = Decoder.Decode<string>(BString.Encode("hello"));
            output.WriteLine(s);
            var i = Decoder.Decode<int>(Number.Encode(111));
            output.WriteLine($"{i}");

            var metaInfo = Decoder.Decode<MetaInfo>(
                System.IO.File.ReadAllBytes(@"../../../manti.torrent"));
            output.WriteLine(metaInfo.ToString());
        }
    }
}
