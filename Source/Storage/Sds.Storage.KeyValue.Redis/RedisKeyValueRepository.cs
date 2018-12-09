using Newtonsoft.Json;
using Sds.Storage.KeyValue.Core;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sds.Storage.KeyValue.Redis
{
    public class RedisKeyValueRepository : IKeyValueRepository
    {
        private const int StreamPortionSize = 15 * 1024 * 1024;

        private ConfigurationOptions Config;

        public RedisKeyValueRepository(string connectionString, int syncTimeout, int connectTimeout, int responseTimeout)
        {
            var connection = Environment.ExpandEnvironmentVariables(connectionString);
            Config = ConfigurationOptions.Parse(connection.ToString());
            Config.SyncTimeout = syncTimeout;
            Config.ConnectTimeout = connectTimeout;
            Config.ResponseTimeout = responseTimeout;
        }

        public byte[] LoadData(Guid id)
        {
            return LoadData(id.ToString());
        }

        public void SaveData(Guid id, string value)
        {
            SaveData(id.ToString(), value);
        }

        public void SaveData(Guid id, byte[] value)
        {
            SaveData(id.ToString(), value);
        }

        public void SaveStream(Guid id, Stream stream)
        {
            SaveStream(id.ToString(), stream);
        }

        public void LoadStream(Guid id, Stream stream)
        {
            var rawPortions = LoadData(id);
            if (rawPortions == null)
            {
                return;
            }

            var portions = JsonConvert.DeserializeObject<List<PortionInfo>>(Encoding.UTF8.GetString(rawPortions));
            foreach (var portionInfo in portions.OrderBy(x => x.Order))
            {
                var portion = LoadData(portionInfo.BlobId);
                stream.Write(portion, 0, portion.Length);
            }
        }

        public void DeleteStream(Guid id)
        {
            var rawPortions = LoadData(id);
            if (rawPortions == null)
            {
                return;
            }

            var portions = JsonConvert.DeserializeObject<List<PortionInfo>>(Encoding.UTF8.GetString(rawPortions));
            foreach (var portionInfo in portions)
            {
                Delete(portionInfo.BlobId);
            }

            Delete(id);
        }

        public T LoadObject<T>(Guid id) where T : class
        {
            var data = LoadData(id);
            if (data == null)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));
        }

        public void SaveObject<T>(Guid id, T obj) where T : class
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var value = JsonConvert.SerializeObject(obj);

            SaveData(id, value);
        }

        public void Delete(Guid id)
        {
            Delete(id.ToString());
        }

        public void Delete(string id)
        {
            using (var connect = ConnectionMultiplexer.Connect(Config))
            {
                var database = connect.GetDatabase();
                database.KeyDelete(id);
            }
        }

        public void SetExpiration(Guid id, TimeSpan expiry)
        {
            SetExpiration(id.ToString(), expiry);
        }

        public void SetStreamExpiration(Guid id, TimeSpan expiry)
        {
            var rawPortions = LoadData(id);
            if (rawPortions == null)
            {
                return;
            }

            using (var connect = ConnectionMultiplexer.Connect(Config))
            {
                var database = connect.GetDatabase();
                var portions = JsonConvert.DeserializeObject<List<PortionInfo>>(Encoding.UTF8.GetString(rawPortions));
                foreach (var portionInfo in portions)
                {
                    database.KeyExpire(portionInfo.BlobId.ToString(), expiry);
                }

                database.KeyExpire(id.ToString(), expiry);
            }
        }

        public byte[] LoadData(string id)
        {
            using (var connect = ConnectionMultiplexer.Connect(Config))
            {
                var database = connect.GetDatabase();
                return database.StringGet(id);
            }
        }

        public void SaveData(string id, byte[] value)
        {
            using (var connect = ConnectionMultiplexer.Connect(Config))
            {
                var database = connect.GetDatabase();
                database.StringSet(id, value);
            }
        }

        public void SaveData(string id, string value)
        {
            using (var connect = ConnectionMultiplexer.Connect(Config))
            {
                var database = connect.GetDatabase();
                database.StringSet(id, value);
            }
        }

        public void SetExpiration(string id, TimeSpan expiry)
        {
            using (var connect = ConnectionMultiplexer.Connect(Config))
            {
                var database = connect.GetDatabase();
                database.KeyExpire(id.ToString(), expiry);
            }
        }

        public void SaveStream(string id, Stream stream)
        {
            var portions = new List<PortionInfo>();

            stream.Position = 0;
            while (stream.Position != stream.Length)
            {
                var portionInfo = new PortionInfo
                {
                    BlobId = id,
                    Order = portions.Count
                };

                var portionSize = (int)Math.Min(stream.Length - stream.Position, StreamPortionSize);
                var buffer = new byte[portionSize];
                stream.Read(buffer, 0, portionSize);

                SaveData(portionInfo.BlobId, buffer);
                portions.Add(portionInfo);
            }

            var rawPortions = JsonConvert.SerializeObject(portions);
            SaveData(id, rawPortions);
        }
    }
}
