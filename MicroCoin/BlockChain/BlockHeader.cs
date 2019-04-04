using MicroCoin.Cryptography;
using MicroCoin.Utils;
using System.IO;
using System.Text;

namespace MicroCoin.BlockChain
{
    public class BlockHeader
    {
        public byte BlockSignature { get; set; } = 3;
        public ushort ProtocolVersion { get; set; }
        public ushort AvailableProtocol { get; set; }
        public uint BlockNumber { get; set; }
        public ECKeyPair AccountKey { get; set; }
        public Currency Reward { get; set; }
        public Currency Fee { get; set; }
        public Timestamp Timestamp { get; set; }
        public uint CompactTarget { get; set; }
        public int Nonce { get; set; }
        public ByteString Payload { get; set; }
        public Hash CheckPointHash { get; set; }
        public Hash TransactionHash { get; set; }
        public Hash ProofOfWork { get; set; }
        internal BlockHeader(Stream s)
        {
            using (var br = new BinaryReader(s, Encoding.ASCII, true))
            {                
                BlockSignature = br.ReadByte();
                if (BlockSignature > 0)
                {
                    ProtocolVersion = br.ReadUInt16();
                    AvailableProtocol = br.ReadUInt16();
                }
                BlockNumber = br.ReadUInt32();
                AccountKey = new ECKeyPair();
                AccountKey.LoadFromStream(s);
                Reward = br.ReadUInt64();
                Fee = br.ReadUInt64();
                Timestamp = br.ReadUInt32();
                CompactTarget = br.ReadUInt32();
                Nonce = br.ReadInt32();
                ushort stringLength = br.ReadUInt16();
                if (stringLength > 0)
                {
                    Payload = br.ReadBytes(stringLength);
                }
                stringLength = br.ReadUInt16();
                if (stringLength > 0)
                {
                    CheckPointHash = br.ReadBytes(stringLength);
                }
                stringLength = br.ReadUInt16();
                if (stringLength > 0)
                {
                    TransactionHash = br.ReadBytes(stringLength);
                }
                stringLength = br.ReadUInt16();
                if (stringLength > 0)
                {
                    ProofOfWork = br.ReadBytes(stringLength);
                }
            }
        }

        internal BlockHeader()
        {
            AccountKey = new ECKeyPair();
        }

        internal virtual void SaveToStream(Stream s)
        {
            using (var bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                bw.Write(BlockSignature);
                bw.Write(ProtocolVersion);
                bw.Write(AvailableProtocol);
                bw.Write(BlockNumber);
                if (AccountKey == null)
                {
                    bw.Write((ushort)6);
                    bw.Write((ushort)0);
                    bw.Write((ushort)0);
                    bw.Write((ushort)0);
                }
                else
                {
                    AccountKey.SaveToStream(s);
                }
                bw.Write(Reward);
                bw.Write(Fee);
                bw.Write(Timestamp);
                bw.Write(CompactTarget);
                bw.Write(Nonce);
                Payload.SaveToStream(bw);
                CheckPointHash.SaveToStream(bw);
                TransactionHash.SaveToStream(bw);
                ProofOfWork.SaveToStream(bw);
            }
        }
    }
}
