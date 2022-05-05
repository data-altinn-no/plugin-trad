using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Altinn.Dan.Plugin.Trad.Models;
using Nadobe.Common.Models;

namespace Altinn.Dan.Plugin.Trad
{
    public static class Helpers
    {
        private static string KEY_PREFIX = "trad-entry-";

        public static string GetCacheKeyForSsn(string ssn)
        {
            using var sha256 = SHA256.Create();
            return KEY_PREFIX + Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(ssn)));
        }

        public static PersonExternal MapInternalModelToExternal(PersonInternal personInternal)
        {
            var person = new PersonExternal
            {
                Ssn = personInternal.Ssn,
                Title = personInternal.Title,
                IsAssociatedWithAuditedBusiness = personInternal.IsAssociatedWithAuditedBusiness,
            };

            if (personInternal.AuthorizedRepresentatives != null)
            {
                person.AuthorizedRepresentatives = new List<PersonExternal>();
                foreach (var associate in personInternal.AuthorizedRepresentatives)
                {
                    person.AuthorizedRepresentatives.Add(MapInternalModelToExternal(associate));
                }
            }

            if (personInternal.IsaAuthorizedRepresentativeFor != null)
            {
                person.IsaAuthorizedRepresentativeFor = new List<PersonExternal>();
                foreach (var principal in personInternal.IsaAuthorizedRepresentativeFor)
                {
                    person.IsaAuthorizedRepresentativeFor.Add(MapInternalModelToExternal(principal));
                }
            }

            return person;
        }
    }
}
