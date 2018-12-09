using System;
using System.Collections.Generic;
using Sds.Storage.KeyValue.Core;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.IO;

namespace Sds.Storage.KeyValue.InMemory
{
    public class InMemoryKeyValueRepository : IKeyValueRepository
    {
        private Dictionary<string, CacheItem> storage = new Dictionary<string, CacheItem>();

        public void Delete(Guid id)
        {
            if (storage.ContainsKey(id.ToString()))
            {
                storage.Remove(id.ToString());
            }
        }

        public void Delete(string id)
        {
            if (storage.ContainsKey(id))
            {
                storage.Remove(id);
            }
        }

        public void DeleteStream(Guid id)
        {
            if (storage.ContainsKey(id.ToString()))
            {
                storage.Remove(id.ToString());
            }
        }

        public byte[] LoadData(Guid id)
        {
            if (storage.ContainsKey(id.ToString()))
            {
                if (DateTimeOffset.Now - storage[id.ToString()].Created >= storage[id.ToString()].ExpiresAfter)
                {
                    storage.Remove(id.ToString());
                    return null;
                }
                return storage[id.ToString()].Value;
            }
            return null;
        }

        public byte[] LoadData(string id)
        {
            if (storage.ContainsKey(id))
            {
                if (DateTimeOffset.Now - storage[id].Created >= storage[id].ExpiresAfter)
                {
                    storage.Remove(id);
                    return null;
                }
                return storage[id].Value;
            }
            return null;
        }

        public T LoadObject<T>(Guid id) where T : class
        {
            var data = storage[id.ToString()].Value;
            if (data == null)
                return default(T);
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(data))
            {
                object obj = bf.Deserialize(ms);
                return (T)obj;
            }
        }

        public void LoadStream(Guid id, Stream stream)
        {
            var bytes = storage[id.ToString()].Value;
            stream.Write(bytes, 0, bytes.Length);
        }

        public void SaveData(Guid id, byte[] value)
        {
            storage[id.ToString()] = new CacheItem(value);
        }

        public void SaveData(Guid id, string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value);
            storage[id.ToString()] = new CacheItem(bytes);
        }

        public void SaveData(string id, byte[] value)
        {
            storage[id] = new CacheItem(value);
        }

        public void SaveData(string id, string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value);
            storage[id] = new CacheItem(bytes);
        }

        public void SaveObject<T>(Guid id, T descriptor) where T : class
        {
            if (descriptor == null)
                return;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, descriptor);
                storage[id.ToString()] = new CacheItem(ms.ToArray());
            }
        }

        public void SaveStream(Guid id, Stream stream)
        {
            byte[] buffer = new byte[16 * 1024];

            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                storage[id.ToString()] = new CacheItem(ms.ToArray());
            }
        }

        public void SaveStream(string id, Stream stream)
        {
            byte[] buffer = new byte[16 * 1024];

            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                storage[id] = new CacheItem(ms.ToArray());
            }
        }

        public void SetExpiration(Guid id, TimeSpan expiry)
        {
            storage[id.ToString()].ExpiresAfter = expiry;
        }

        public void SetExpiration(string id, TimeSpan expiry)
        {
            storage[id].ExpiresAfter = expiry;
        }

        public void SetStreamExpiration(Guid id, TimeSpan expiry)
        {
            storage[id.ToString()].ExpiresAfter = expiry;
        }
    }
}
