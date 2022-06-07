using System.Collections.Generic;
using Altinn.Dan.Plugin.Trad.Models;
using Nadobe.Common.Interfaces;
using Nadobe.Common.Models;
using Nadobe.Common.Models.Enums;
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
                            EvidenceValueName = "erTilknyttetVirksomhetMedRevisjonsPlikt",
                            ValueType = EvidenceValueType.Boolean
                        },
                        new()
                        {
                            EvidenceValueName = "tittel",
                            ValueType = EvidenceValueType.String
                        }
                    },
                    Parameters = new List<EvidenceParameter>
                    {
                        new()
                        {
                            EvidenceParamName = "inkluderPersonerUtenTilknytningTilVirksomhetMedRevisjonsplikt",
                            ParamType = EvidenceParamType.Boolean,
                            Required = false
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
                }
            };

        }
    }
}
