using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MicroCoin.Types;

namespace MicroCoin.Cryptography
{
    public interface ICryptoService : IDisposable
    {
        ByteString DecryptString(Hash em, ECKeyPair myKey, ECPoint otherKey);
        Hash DoubleSha256(Hash data);
        Hash EncryptString(ByteString data, ECKeyPair myKey, ECPoint otherKey);
        byte[] GenerateSharedKey(ECKeyPair myKey, ECPoint otherKey);
        ECSignature GenerateSignature(Hash data, ECKeyPair keyPair);
        Task<ECSignature> GenerateSignatureAsync(Hash data, ECKeyPair keyPair);
        Hash RipeMD160(Hash data);
        Hash Sha256(Hash data);
        bool ValidateSignature(Hash data, ECSignature signature, ECKeyPair keyPair);
        Hash Sha256(Stream hashBuffer);
    }
}