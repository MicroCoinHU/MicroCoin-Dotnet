using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MicroCoin.Common
{
    public interface IStreamSerializable
    {
        void SaveToStream(Stream stream);
        void LoadFromStream(Stream stream);
    }
}
