using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Trad.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Dan.Plugin.Trad.Test;

public class MockCache : IDistributedCache
{
    private Dictionary<string, PersonInternal> _backingStore;

    public MockCache()
    {
        _backingStore = new Dictionary<string, PersonInternal>();
    }

    public Dictionary<string, PersonInternal> GetAll()
    {
        return _backingStore;
    }

    public byte[] Get(string key)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_backingStore[key]));
    }

    public async Task<byte[]> GetAsync(string key, CancellationToken token = new())
    {
        return await Task.FromResult(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_backingStore[key])));
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        _backingStore[key] = JsonConvert.DeserializeObject<PersonInternal>(Encoding.UTF8.GetString(value));
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
        CancellationToken token = new())
    {
        _backingStore[key] = JsonConvert.DeserializeObject<PersonInternal>(Encoding.UTF8.GetString(value));
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