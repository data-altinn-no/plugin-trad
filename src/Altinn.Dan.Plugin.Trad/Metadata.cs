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
            return new List<EvidenceCode>()
            {
                new()
                {
                    EvidenceCodeName = "VerifiserAdvokat",
                    EvidenceSource = Source,
                    BelongsToServiceContexts = new List<string> { "Advokatregisteret" },
                    Values = new List<EvidenceValue>()
                    {
                        new()
                        {
                            EvidenceValueName = "Fodselsnummer",
                            ValueType = EvidenceValueType.String
                        },
                        new()
                        {
                            EvidenceValueName = "ErRegistrert",
                            ValueType = EvidenceValueType.Boolean
                        },
                        new()
                        {
                            EvidenceValueName = "Tittel",
                            ValueType = EvidenceValueType.String
                        }
                    }
                },
                new()
                {
                    EvidenceCodeName = "HentAdvokatRegisterPerson",
                    EvidenceSource = Source,
                    BelongsToServiceContexts = new List<string> { "Advokatregisteret" },
                    Values = new List<EvidenceValue>()
                    {
                        new()
                        {
                            EvidenceValueName = "default",
                            ValueType = EvidenceValueType.JsonSchema,
                            JsonSchemaDefintion = JsonSchema.FromType<Person>().ToJson(Formatting.None),
                        }
                    },
                    AuthorizationRequirements = new List<Requirement>()
                    {
                        new MaskinportenScopeRequirement()
                        {
                            RequiredScopes = new List<string>() { "altinn:dataaltinnno/advokatreg-person" }
                        }
                    }
                }
            };

        }
    }
}
