using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Altinn.Dan.Plugin.Trad.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.Dan.Plugin.Trad;

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
    
    public static PersonPrivate MapInternalModelToPrivate(PersonInternal personInternal)
    {
        return MapInternalPersonToPrivate(personInternal, true);
    }

    public static bool ShouldRunUpdate(DateTime? timeToCheck = null)
    {
        var norwegianTime = timeToCheck ?? TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "W. Europe Standard Time");


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
            Title = (TitleTypeExternal)personInternal.Title,
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
            AuthorizedRepresentatives =
                MapInternalPersonListToExternal(practiceInternal.AuthorizedRepresentatives, false),
            IsaAuthorizedRepresentativeFor =
                MapInternalPersonListToExternal(practiceInternal.IsAnAuthorizedRepresentativeFor, false),
            SubOrganizationNumber = practiceInternal.SubOrganizationNumber,
            MainPractice = practiceInternal.MainPractice
        };
    }

    public static ZipBulkPerson MapInternalPersonToZipBulk(PersonInternal personInternal)
    {
        if(personInternal == null) return null;

        var zipBulkPerson = new ZipBulkPerson()
        {
            RegistrationNumber = personInternal.RegistrationNumber,
            Ssn = personInternal.Ssn,
            Firstname = personInternal.Firstname,
            LastName = personInternal.LastName,
            Title = personInternal.Title,
            Practices = personInternal.Practices?
                .Where(p => p is not null)
                .Select(MapInternalPracticeToZipBulk)
                .ToList()
        };

        return zipBulkPerson;
    }

    private static ZipBulkPractice MapInternalPracticeToZipBulk(PracticeInternal practiceInternal)
    {
        if(practiceInternal == null) return null;

        var zipBulkPractice = new ZipBulkPractice
        {
            CompanyNumber = practiceInternal.CompanyNumber,
            OrganizationNumber = practiceInternal.OrganizationNumber,
            AuthorizedRepresentatives =
                practiceInternal.AuthorizedRepresentatives?
                    .Where(p => p is not null)
                    .Select(MapInternalPersonToZipBulk)
                    .ToList(),
            IsAnAuthorizedRepresentativeFor =
                practiceInternal.IsAnAuthorizedRepresentativeFor?
                    .Where(p => p is not null)
                    .Select(MapInternalPersonToZipBulk)
                    .ToList(),
            SubOrganizationNumber = practiceInternal.SubOrganizationNumber,
            MainPractice = practiceInternal.MainPractice
        };

        return zipBulkPractice;
    }
    
    private static List<PersonPrivate> MapInternalPersonListToPrivate(List<PersonInternal> personInternals,
        bool descendIntoPractices)
    {
        if (personInternals == null || personInternals.Count == 0) return null;
        var personList = new List<PersonPrivate>();
        foreach (var personInternal in personInternals)
        {
            personList.Add(MapInternalPersonToPrivate(personInternal, descendIntoPractices));
        }

        return personList;
    }

    private static PersonPrivate MapInternalPersonToPrivate(PersonInternal personInternal,
        bool descendIntoPractices)
    {
        var person = new PersonPrivate
        {
            FirstName = personInternal.Firstname,
            LastName = personInternal.LastName,
            Title = (TitleTypeExternal)personInternal.Title,
        };

        if (descendIntoPractices)
        {
            person.Practices = MapInternalPracticeListToPrivate(personInternal.Practices);
        }

        return person;
    }

    private static List<PracticePrivate> MapInternalPracticeListToPrivate(List<PracticeInternal> practiceInternals)
    {
        var practiceList = new List<PracticePrivate>();
        foreach (var practice in practiceInternals)
        {
            practiceList.Add(MapInternalPracticeToPrivate(practice));
        }

        return practiceList;
    }

    private static PracticePrivate MapInternalPracticeToPrivate(PracticeInternal practiceInternal)
    {
        return new PracticePrivate
        {
            OrganizationNumber = practiceInternal.OrganizationNumber,
            AuthorizedRepresentatives =
                MapInternalPersonListToPrivate(practiceInternal.AuthorizedRepresentatives, false),
            IsaAuthorizedRepresentativeFor =
                MapInternalPersonListToPrivate(practiceInternal.IsAnAuthorizedRepresentativeFor, false),
            SubOrganizationNumber = practiceInternal.SubOrganizationNumber,
            MainPractice = practiceInternal.MainPractice
        };
    }
}

/// <summary>
/// Logger extension
/// </summary>
public static class LoggerExtension
{
    /// <summary>
    /// Create a timer that writes the run time of the operation to the log 
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="metricName">Name of metric used in logging.</param>
    /// <returns>A Stopwatch Log that writes to the log when it is disposed</returns>
    public static StopwatchLog Timer(this ILogger logger, string metricName)
    {
        return new StopwatchLog(logger, metricName);
    }
}

/// <summary>
/// Single instance log container
/// </summary>
public class StopwatchLog : IDisposable
{
    /// <summary>
    /// The amount of milliseconds elapsed since start
    /// </summary>
    public long ElapsedMilliseconds => Stopwatch.ElapsedMilliseconds;

    private ILogger Logger { get; }

    private Stopwatch Stopwatch { get; }

    private string Logtext { get; }

    /// <summary>
    /// Create a new stopwatch log
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="text">Text to write to log. Replaces {0} with runtime until dispose.</param>
    public StopwatchLog(ILogger logger, string text)
    {
        Logger = logger;
        Stopwatch = Stopwatch.StartNew();
        Logtext = text;
    }

    /// <summary>
    /// Dispose of the Log
    /// </summary>
    public void Dispose()
    {
        Stopwatch.Stop();
        var elapsedMilliseconds = Stopwatch.ElapsedMilliseconds;
        Logger?.LogInformation("[NadobeTimer] {logtext} elapsedMs={elapsedMs}", Logtext, elapsedMilliseconds);
    }
}