using MicroCoin.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace MicroCoin.Common
{
    public class HashList : IList<Hash>, IDisposable
    {
        protected class HashListEnumerator : IEnumerator, IEnumerator<Hash>
        {
            private int index = 0;
            private readonly HashList list;
            public HashListEnumerator(HashList list)
            {
                this.list = list;
            }
            public object Current => list.Get(index);

            Hash IEnumerator<Hash>.Current => list.Get(index);

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            public bool MoveNext()
            {
                index++;
                return index > list.Count;
            }

            public void Reset()
            {
                index = 0;
            }
        }

        private readonly MemoryStream memoryStream = new MemoryStream(1024 * 1024 * 32);
        private readonly int hashSize = 32;
        public HashList()
        {

        }

        protected void Set(int index, Hash hash)
        {
            memoryStream.Position = index * hashSize;
            memoryStream.Write(hash, 0, hashSize);
        }
        protected Hash Get(int index)
        {
            memoryStream.Position = index * hashSize;
            byte[] buffer = new byte[hashSize];
            memoryStream.Read(buffer, 0, hashSize);
            return buffer;
        }

        public Hash this[int index] { get => Get(index); set => Set(index, value); }

        public int Count => (int) (memoryStream.Length / hashSize);

        public bool IsReadOnly => false;

        public void Add(Hash item)
        {
            memoryStream.Seek(0, SeekOrigin.End);
            memoryStream.Write(item, 0, hashSize);
        }

        public void Clear()
        {
            memoryStream.SetLength(0);
        }

        public bool Contains(Hash item)
        {
            return false;
        }

        public void CopyTo(Hash[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<Hash> GetEnumerator()
        {
            return new HashListEnumerator(this);
        }

        public int IndexOf(Hash item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, Hash item)
        {
            memoryStream.Position = index * hashSize;
            memoryStream.Write(item, 0, hashSize);
        }

        public bool Remove(Hash item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new HashListEnumerator(this);
        }

        public Stream GetBuffer()
        {
            memoryStream.Position = 0;
            return memoryStream;
        }

        public void Dispose()
        {
            memoryStream.Dispose();
            GC.SuppressFinalize(this);
        }

        public void AddRange(List<Hash> checkPointHash)
        {
            checkPointHash.ForEach((item) => Add(item));
        }
    }
}
