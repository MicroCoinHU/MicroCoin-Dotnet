using MicroCoin.Cryptography;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroCoin.KeyStore
{
    public interface IKeyStore
    {
        void Add(KeyPair keyPair);
        IEnumerable<KeyPair> All();
        int Count();
        void Remove(KeyPair keyPair);
    }
}
