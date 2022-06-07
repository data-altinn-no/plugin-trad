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

        public static PersonExternal MapInternalModelToExternal(PersonInternal personInternal)
        {
            return MapInternalPersonToExternal(personInternal, true);
        }

        public static bool ShouldRunUpdate(DateTime? timeToCheck = null)
        {
            var norwegianTime = timeToCheck ?? TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Oslo");


            // Always update between 0600 and 1759
            if (norwegianTime.Hour is >= 6 and < 18)
            {
                return true;
            }

            // Don't update at all between 0100 and 0459
            if (norwegianTime.Hour is >= 1 and < 5)
            {
                return false;
            }

            // At 0400-0559 and 1759-0100 update every half hour. This assumes this is ran at most every 5 minutes.
            if (norwegianTime.Minute is >= 58 or <= 2 or >= 28 and <= 32)
            {
                return true;
            }

            return false;

        }

        private static List<PersonExternal> MapInternalPersonListToExternal(List<PersonInternal> personInternals,
            bool descendIntoPractices)
        {
            if (personInternals == null || personInternals.Count == 0) return null;
            var personList = new List<PersonExternal>();
            foreach (var personInternal in personInternals)
            {
                personList.Add(MapInternalPersonToExternal(personInternal, descendIntoPractices));
            }

            return personList;
        }

        private static PersonExternal MapInternalPersonToExternal(PersonInternal personInternal,
            bool descendIntoPractices)
        {
            var person = new PersonExternal
            {
                Ssn = personInternal.Ssn,
                Title = personInternal.Title,
            };

            if (descendIntoPractices)
            {
                person.Practices = MapInternalPracticeListToExternal(personInternal.Practices);
            }

            return person;
        }

        private static List<PracticeExternal> MapInternalPracticeListToExternal(List<PracticeInternal> practiceInternals)
        {
            var practiceList = new List<PracticeExternal>();
            foreach (var practice in practiceInternals)
            {
                practiceList.Add(MapInternalPracticeToExternal(practice));
            }

            return practiceList;
        }

        private static PracticeExternal MapInternalPracticeToExternal(PracticeInternal practiceInternal)
        {
            return new PracticeExternal
            {
                OrganizationNumber = practiceInternal.OrganizationNumber,
                Auditable = !practiceInternal.AuditExcempt,
                AuthorizedRepresentatives =
                    MapInternalPersonListToExternal(practiceInternal.AuthorizedRepresentatives, false),
                IsaAuthorizedRepresentativeFor =
                    MapInternalPersonListToExternal(practiceInternal.IsAnAuthorizedRepresentativeFor, false),
            };
        }
    }
}
