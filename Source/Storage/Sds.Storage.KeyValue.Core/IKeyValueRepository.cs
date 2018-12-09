using System;
using System.IO;

namespace Sds.Storage.KeyValue.Core
{
    public interface IKeyValueRepository
    {
        byte[] LoadData(Guid id);
        byte[] LoadData(string id);
        void SaveData(Guid id, byte[] value);
        void SaveData(string id, byte[] value);
        void SaveData(Guid id, string value);
        void SaveData(string id, string value);
        void SaveStream(Guid id, Stream stream);
        void SaveStream(string id, Stream stream);
        void LoadStream(Guid id, Stream stream);
        void DeleteStream(Guid id);
        T LoadObject<T>(Guid id) where T : class;
        void SaveObject<T>(Guid id, T descriptor) where T : class;
        void Delete(Guid id);
        void Delete(string id);
        void SetExpiration(Guid id, TimeSpan expiry);
        void SetExpiration(string id, TimeSpan expiry);
        void SetStreamExpiration(Guid id, TimeSpan expiry);
    }
}
