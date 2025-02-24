using System;
using System.Text;
using System.Threading.Tasks;
using Dan.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Altinn.Dan.Plugin.Trad.Services;

public interface IOrganizationService
{
    Task<string> GetOrgName(int orgNr);
}

public class OrganizationService(IEntityRegistryService entityRegistryService, IDistributedCache cache)
    : IOrganizationService
{
    private const string OrganizationCachePrefix = "trad-org";

    public async Task<string> GetOrgName(int orgNr)
    {
        var key = GetCacheKey(orgNr);
        var result = await cache.GetAsync(key);
        if (result != null)
        {
            return Encoding.UTF8.GetString(result);
        }

        var practiceOrg = await entityRegistryService.GetFull(orgNr.ToString());
        if (practiceOrg?.Navn is null) return null;

        await cache.SetAsync(key, Encoding.UTF8.GetBytes(practiceOrg.Navn), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });
        return practiceOrg.Navn;

    }
    

    private static string GetCacheKey(int orgNr) => $"{OrganizationCachePrefix}{orgNr}";
}