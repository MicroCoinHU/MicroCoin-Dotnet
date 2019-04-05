using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MicroCoin.BlockChain
{
    public class BlockChainFileStorage : IBlockChain, IDisposable
    {
        private readonly FileStream fileStream;
        private readonly FileStream indexStream;

        public BlockChainFileStorage()
        {
            fileStream = new FileStream("blocks.chain", FileMode.OpenOrCreate);
            indexStream = new FileStream("blocks.idx", FileMode.OpenOrCreate);
        }

        public int Count => throw new NotImplementedException();

        public void AddBlock(Block block)
        {
            using (BinaryWriter bw = new BinaryWriter(indexStream, Encoding.ASCII, true))
            {
                bw.Write(block.Header.BlockNumber);
                long pos = fileStream.Position;
                bw.Write(pos);
                block.SaveToStream(fileStream);
                bw.Write(fileStream.Position - pos);
            }
        }

        public void Dispose()
        {
            fileStream.Dispose();
            indexStream.Dispose();
        }

        public Block GetBlock(uint blockNumber)
        {
            throw new NotImplementedException();
        }
    }
}
