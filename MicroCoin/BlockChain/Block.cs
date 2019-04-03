using MicroCoin.Transactions;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MicroCoin.BlockChain
{
    public class Block
    {
        public BlockHeader Header { get; set; }
        public IList<ITransaction> Transactions { get; set; }
        public void LoadFromStream(Stream stream)
        {
            Header = new BlockHeader(stream);
            if (Header.BlockSignature == 1 || Header.BlockSignature == 3)
            {
                return;
            }
            using (var br = new BinaryReader(stream, Encoding.Default, true))
            {
                var TransactionCount = br.ReadUInt32();
                if (TransactionCount <= 0) return;
                Transactions = new List<ITransaction>();
                for (var i = 0; i < TransactionCount; i++)
                {
                    var transactionType = (TransactionType)br.ReadUInt32();
                    ITransaction t;
                    t = TransactionFactory.FromType(transactionType);
                    t.TransactionType = transactionType;
                    t.LoadFromStream(stream);
                    Transactions.Add(t);
                }
            }
        }
        internal void SaveToStream(Stream stream)
        {
            Header.SaveToStream(stream);
            if (Header.BlockSignature == 1 || Header.BlockSignature == 3)
            {
                return;
            }
            using (var bw = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                if (Transactions == null)
                {
                    bw.Write((uint)0);
                    return;
                }
                bw.Write((uint)Transactions.Count);
                foreach (var t in Transactions)
                {
                    bw.Write((uint)t.TransactionType);
                    t.SaveToStream(stream);
                }
            }
        }
    }
}
