using System;
using System.Collections.Generic;

namespace CareOps.Decisions.Calendars
{
    /// <summary>Bounded date arithmetic, not policy selection or decision authority.</summary>
    public static class Deadlines
    {
        private const int MaximumCalendarDays = 3660;

        /// <summary>
        /// Count approved business days AFTER start; zero returns start unchanged.
        /// All dates are civil dates, not timestamps. No holidays or weekends are inferred.
        /// </summary>
        public static DateTime BusinessDaysAfter(
            DateTime start, int days, DateTime coverageFrom, DateTime coverageUntilExclusive,
            IReadOnlyCollection<DayOfWeek> nonWorkingWeekdays,
            IReadOnlyCollection<DateTime> closures)
        {
            if (nonWorkingWeekdays == null)
                throw new ArgumentNullException(nameof(nonWorkingWeekdays));
            if (closures == null)
                throw new ArgumentNullException(nameof(closures));
            RequireCivilDate(start, nameof(start));
            RequireCivilDate(coverageFrom, nameof(coverageFrom));
            RequireCivilDate(coverageUntilExclusive, nameof(coverageUntilExclusive));
            if (days < 0 || days > MaximumCalendarDays)
                throw new ArgumentOutOfRangeException(nameof(days));
            if (coverageUntilExclusive <= coverageFrom ||
                (coverageUntilExclusive - coverageFrom).TotalDays > MaximumCalendarDays)
                throw new ArgumentException("A bounded, nonempty calendar coverage period is required.");
            if (start < coverageFrom || start >= coverageUntilExclusive)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (nonWorkingWeekdays.Count > 6 || closures.Count > MaximumCalendarDays)
                throw new ArgumentException("Invalid or oversized nonworking-day configuration.");

            var weekdays = new HashSet<DayOfWeek>();
            foreach (DayOfWeek weekday in nonWorkingWeekdays)
            {
                if ((int)weekday < 0 || (int)weekday > 6 || !weekdays.Add(weekday))
                    throw new ArgumentException("Invalid or duplicate nonworking weekday.", nameof(nonWorkingWeekdays));
            }
            var closedDates = new HashSet<DateTime>();
            foreach (DateTime closure in closures)
            {
                RequireCivilDate(closure, nameof(closures));
                if (closure < coverageFrom || closure >= coverageUntilExclusive || !closedDates.Add(closure))
                    throw new ArgumentException("Closure is outside coverage or duplicated.", nameof(closures));
            }

            DateTime result = start;
            int remaining = days;
            // Coverage bounds both runtime and knowledge; never extend an unknown calendar.
            while (remaining > 0)
            {
                if (result >= coverageUntilExclusive.AddDays(-1))
                    throw new InvalidOperationException("Calendar coverage exhausted; request a reviewed extension.");
                result = result.AddDays(1);
                if (!weekdays.Contains(result.DayOfWeek) && !closedDates.Contains(result))
                    remaining--;
            }
            return result;
        }

        /// <summary>
        /// Add calendar months with explicit end-of-month clamping, never 30-day multiplication.
        /// The caller must obtain duration and counting convention from an approved rule.
        /// </summary>
        public static DateTime CalendarMonthsAfter(DateTime start, int months)
        {
            RequireCivilDate(start, nameof(start));
            if (months < 0 || months > 120)
                throw new ArgumentOutOfRangeException(nameof(months));
            return start.AddMonths(months);
        }

        private static void RequireCivilDate(DateTime value, string parameter)
        {
            if (value.Kind != DateTimeKind.Unspecified || value.TimeOfDay != TimeSpan.Zero)
                throw new ArgumentException("A timezone-resolved civil date at midnight is required.", parameter);
        }
    }
}
