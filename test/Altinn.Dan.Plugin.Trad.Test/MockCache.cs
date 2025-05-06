using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Trad.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Trad.Test
{
    public class MockCache : IDistributedCache
    {
        private Dictionary<string, byte[]> _backingStore;

        public MockCache()
        {
            _backingStore = new Dictionary<string, byte[]>();
        }

        // Not part of IDistributedCache
        public Dictionary<string, PersonInternal> GetAllPeople()
        {
            var personInternal = new Dictionary<string, PersonInternal>();
            foreach (var item in _backingStore)
            {
                try
                {
                    var person = JsonConvert.DeserializeObject<PersonInternal>(Encoding.UTF8.GetString(item.Value));
                    personInternal.Add(item.Key, person!);
                }
                catch (JsonSerializationException)
                {
                    // Continue
                }
            }
            return personInternal;
        }

        public byte[] Get(string key)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_backingStore[key]));
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = new())
        {
            if (!_backingStore.ContainsKey(key))
            {
                return default;
            }
            return await Task.FromResult(_backingStore[key]);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _backingStore[key] = value;
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = new())
        {
            _backingStore[key] = value;
            await Task.CompletedTask;
        }

        public void Refresh(string key)
        {
            throw new NotImplementedException();
        }

        public Task RefreshAsync(string key, CancellationToken token = new())
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(string key, CancellationToken token = new())
        {
            throw new NotImplementedException();
        }
    }
}
