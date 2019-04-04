using System.IO;

namespace MicroCoin.Common
{
    public interface IStreamSerializable
    {
        void SaveToStream(Stream stream);
        void LoadFromStream(Stream stream);
    }
}
