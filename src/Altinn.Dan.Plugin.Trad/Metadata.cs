using System.Collections.Generic;
using Altinn.Dan.Plugin.Trad.Models;
using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Newtonsoft.Json;
using NJsonSchema;

namespace Altinn.Dan.Plugin.Trad
{
    public class EvidenceSourceMetadata : IEvidenceSourceMetadata
    {
        public const string Source = "Tilsynsrådet for Advokatvirksomheter";
        public const int ErrorCodeUpstreamError = 1;

        public List<EvidenceCode> GetEvidenceCodes()
        {
            return new List<EvidenceCode>
            {
                new()
                {
                    EvidenceCodeName = "AdvRegPersonVerifikasjon",
                    EvidenceSource = Source,
                    BelongsToServiceContexts = new List<string> { "Advokatregisteret" },
                    RequiredScopes = "dan:external/advreg",
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
                    EvidenceCodeName = "AdvokatverifikasjonPrivat",
                    EvidenceSource = Source,
                    BelongsToServiceContexts = new List<string> { "Advokatregisteret" },
                    RequiredScopes = "dan:external/advreg",
                    Values = new List<EvidenceValue>
                    {
                        new()
                        {
                            EvidenceValueName = "fornavn",
                            ValueType = EvidenceValueType.String
                        },
                        new()
                        {
                            EvidenceValueName = "etternavn",
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
                            RequiredScopes = new List<string> { "altinn:dataaltinnno/advregverifikasjonprivat" }
                        }
                    }
                },
                new()
                {
                    EvidenceCodeName = "AdvRegPerson",
                    EvidenceSource = Source,
                    BelongsToServiceContexts = new List<string> { "Advokatregisteret" },
                    RequiredScopes = "dan:external/advreg",
                    Values = new List<EvidenceValue>
                    {
                        new()
                        {
                            EvidenceValueName = "default",
                            ValueType = EvidenceValueType.JsonSchema,
                            JsonSchemaDefintion = JsonSchema.FromType<PersonExternal>().ToJson(Formatting.None),
                        }
                    },
                    AuthorizationRequirements = new List<Requirement>
                    {
                        new MaskinportenScopeRequirement
                        {
                            RequiredScopes = new List<string> { "altinn:dataaltinnno/advregperson" }
                        }
                    }
                },
                new()
                {
                    EvidenceCodeName = "AdvRegPersonPrivat",
                    EvidenceSource = Source,
                    BelongsToServiceContexts = new List<string> { "Advokatregisteret" },
                    RequiredScopes = "dan:external/advreg",
                    Values = new List<EvidenceValue>
                    {
                        new()
                        {
                            EvidenceValueName = "default",
                            ValueType = EvidenceValueType.JsonSchema,
                            JsonSchemaDefintion = JsonSchema.FromType<PersonPrivate>().ToJson(Formatting.None),
                        }
                    },
                    AuthorizationRequirements = new List<Requirement>
                    {
                        new MaskinportenScopeRequirement
                        {
                            RequiredScopes = new List<string> { "altinn:dataaltinnno/advregpersonprivat" }
                        }
                    }
                },
                new()
                {
                    EvidenceCodeName = "AdvRegBulk.zip",
                    EvidenceSource = Source,
                    BelongsToServiceContexts = new List<string> { "Advokatregisteret" },
                    RequiredScopes = "dan:external/advreg",
                    Values = new List<EvidenceValue>
                    {
                        new()
                        {
                            EvidenceValueName = "default",
                            ValueType = EvidenceValueType.Binary
                        }
                    },
                    AuthorizationRequirements = new List<Requirement>
                    {
                        new MaskinportenScopeRequirement
                        {
                            RequiredScopes = new List<string> { "altinn:dataaltinnno/advregbulk" }
                        }
                    }
                },
                new()
                {
                    EvidenceCodeName = "AdvRegBulkPrivat.zip",
                    EvidenceSource = Source,
                    BelongsToServiceContexts = new List<string> { "Advokatregisteret" },
                    RequiredScopes = "dan:external/advreg",
                    Values = new List<EvidenceValue>
                    {
                        new()
                        {
                            EvidenceValueName = "default",
                            ValueType = EvidenceValueType.Binary
                        }
                    },
                    AuthorizationRequirements = new List<Requirement>
                    {
                        new MaskinportenScopeRequirement
                        {
                            RequiredScopes = new List<string> { "altinn:dataaltinnno/advregbulkprivat" }
                        }
                    }
                }
            };

        }
    }
}
