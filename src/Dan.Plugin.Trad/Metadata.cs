using System.Collections.Generic;
using Altinn.Dan.Plugin.Trad.Models;
using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Newtonsoft.Json.Schema.Generation;

namespace Dan.Plugin.Trad;

public class EvidenceSourceMetadata : IEvidenceSourceMetadata
{
    public const string Source = "Tilsynsrådet for Advokatvirksomheter";
    public const int ErrorCodeUpstreamError = 1;

    public List<EvidenceCode> GetEvidenceCodes()
    {
        JSchemaGenerator generator = new JSchemaGenerator();

        return new List<EvidenceCode>
        {
            new()
            {
                EvidenceCodeName = "AdvRegPersonVerifikasjon",
                EvidenceSource = Source,
                BelongsToServiceContexts = new List<string> { "Advokatregisteret" },
                Values = new List<EvidenceValue>
                {
                    new()
                    {
                        EvidenceValueName = "fodselsnummer",
                        ValueType = EvidenceValueType.String
                    },
                    new()
                    {
                        EvidenceValueName = "verifisert",
                        ValueType = EvidenceValueType.Boolean
                    },
                    new()
                    {
                        EvidenceValueName = "tittel",
                        ValueType = EvidenceValueType.String
                    }
                },
                AuthorizationRequirements = new List<Requirement>
                {
                    new MaskinportenScopeRequirement
                    {
                        RequiredScopes = new List<string> { "altinn:dataaltinnno/advregverifikasjon" }
                    }
                }
            },
            new()
            {
                EvidenceCodeName = "AdvRegPerson",
                EvidenceSource = Source,
                BelongsToServiceContexts = new List<string> { "Advokatregisteret" },
                Values = new List<EvidenceValue>
                {
                    new()
                    {
                        EvidenceValueName = "default",
                        ValueType = EvidenceValueType.JsonSchema,
                        JsonSchemaDefintion = generator.Generate(typeof(PersonExternal)).ToString()
                    }
                },
                AuthorizationRequirements = new List<Requirement>
                {
                    new MaskinportenScopeRequirement
                    {
                        RequiredScopes = new List<string> { "altinn:dataaltinnno/advregperson" }
                    }
                }
            }
        };

    }
}