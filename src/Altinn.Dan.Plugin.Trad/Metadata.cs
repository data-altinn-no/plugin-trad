using System.Collections.Generic;
using Altinn.Dan.Plugin.Trad.Config;
using Microsoft.Extensions.Options;
using Nadobe.Common.Interfaces;
using Nadobe.Common.Models;
using Nadobe.Common.Models.Enums;

namespace Altinn.Dan.Plugin.Trad
{
    public class Metadata : IEvidenceSourceMetadata
    {

        public List<EvidenceCode> GetEvidenceCodes()
        {
            var a = new List<EvidenceCode>()
            {
                new EvidenceCode()
                {
                    EvidenceCodeName = "VerifiserAdvokat",
                    EvidenceSource = EvidenceSourceMetadata.SOURCE,
                    BelongsToServiceContexts = new List<string> { "Advokatregisteret" },
                    Values = new List<EvidenceValue>()
                    {
                        new EvidenceValue()
                        {
                            EvidenceValueName = "Fodselsnummer",
                            ValueType = EvidenceValueType.String
                        },
                        new EvidenceValue()
                        {
                            EvidenceValueName = "ErRegistrert",
                            ValueType = EvidenceValueType.Boolean
                        },
                        new EvidenceValue()
                        {
                            EvidenceValueName = "Tittel",
                            ValueType = EvidenceValueType.String
                        }
                    },
                    AuthorizationRequirements = new List<Requirement>()
                    {
                        new PartyTypeRequirement()
                        {
                            AllowedPartyTypes = new AllowedPartyTypesList()
                            {
                                new KeyValuePair<AccreditationPartyTypes, PartyTypeConstraint>(AccreditationPartyTypes.Requestor,PartyTypeConstraint.PublicAgency)
                            }
                        }
                    }
                }
            };

            return a;
        }
    }

    public class EvidenceSourceMetadata : IEvidenceSourceMetadata
    {
        public const string SOURCE = "Tilsynsrådet for Advokatvirksomheter";

        public const int ERROR_ORGANIZATION_NOT_FOUND = 1;

        public const int ERROR_CCR_UPSTREAM_ERROR = 2;

        public const int ERROR_NO_REPORT_AVAILABLE = 3;

        public const int ERROR_ASYNC_REQUIRED_PARAMS_MISSING = 4;

        public const int ERROR_ASYNC_ALREADY_INITIALIZED = 5;

        public const int ERROR_ASYNC_NOT_INITIALIZED = 6;

        public const int ERROR_AYNC_STATE_STORAGE = 7;

        public const int ERROR_ASYNC_HARVEST_NOT_AVAILABLE = 8;

        public const int ERROR_CERTIFICATE_OF_REGISTRATION_NOT_AVAILABLE = 9;

        private readonly IOptions<ApplicationSettings> _settings;

        public EvidenceSourceMetadata(IOptions<ApplicationSettings> settings)
        {
            _settings = settings;
        }

        public List<EvidenceCode> GetEvidenceCodes()
        {
            return new Metadata().GetEvidenceCodes();
        }
    }
}
