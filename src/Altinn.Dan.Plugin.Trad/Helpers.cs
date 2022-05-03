using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Altinn.Dan.Plugin.Trad.Models;

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

        public static Person MapInternalModelToExternal(PersonInternal personInternal)
        {
            var person = new Person
            {
                Ssn = personInternal.Ssn,
                Title = personInternal.Title,
                IsAssociatedWithAuditedBusiness = personInternal.IsAssociatedWithAuditedBusiness ?? true,
            };

            if (personInternal.AuthorizedRepresentatives != null)
            {
                person.AuthorizedRepresentatives = new List<Person>();
                foreach (var associate in personInternal.AuthorizedRepresentatives)
                {
                    person.AuthorizedRepresentatives.Add(MapInternalModelToExternal(associate));
                }
            }

            if (personInternal.IsaAuthorizedRepresentativeFor != null)
            {
                person.IsaAuthorizedRepresentativeFor = new List<Person>();
                foreach (var principal in personInternal.IsaAuthorizedRepresentativeFor)
                {
                    person.IsaAuthorizedRepresentativeFor.Add(MapInternalModelToExternal(principal));
                }
            }

            return person;
        }
    }
}
