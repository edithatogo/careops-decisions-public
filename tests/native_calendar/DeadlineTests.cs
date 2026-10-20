using System;
using System.Collections.Generic;
using System.Linq;
using CareOps.Decisions.Calendars;
using Xunit;

namespace CareOps.Decisions.Calendars.Tests
{
    public sealed class DeadlineTests
    {
        private static DateTime D(int year, int month, int day) => new DateTime(year, month, day);
        private static readonly DayOfWeek[] Weekend = { DayOfWeek.Saturday, DayOfWeek.Sunday };
        private static readonly DateTime[] None = Array.Empty<DateTime>();
        private static DateTime Calculate(DateTime start, int days,
            IReadOnlyCollection<DateTime>? closures = null) => Deadlines.BusinessDaysAfter(
                start, days, D(2026, 1, 1), D(2026, 2, 1), Weekend, closures ?? None);

        [Fact]
        public void SkipsWeekendsAndAnExplicitClosure()
        {
            var closures = new[] { D(2026, 1, 5) };
            Assert.Equal(D(2026, 1, 6), Calculate(D(2026, 1, 2), 1, closures));
            Assert.Equal(D(2026, 1, 7), Calculate(D(2026, 1, 2), 2, closures));
            Assert.Equal(new[] { D(2026, 1, 5) }, closures);
        }

        [Fact]
        public void ZeroMeansTheStartingDateEvenWhenNonworking()
        {
            Assert.Equal(D(2026, 1, 3), Calculate(D(2026, 1, 3), 0));
        }

        [Fact]
        public void DoesNotInventWeekendOrHolidayRules()
        {
            Assert.Equal(D(2026, 1, 3), Deadlines.BusinessDaysAfter(D(2026, 1, 2), 1,
                D(2026, 1, 1), D(2026, 2, 1), Array.Empty<DayOfWeek>(), None));
        }

        [Fact]
        public void CountsLeapDayAsACivilDate()
        {
            Assert.Equal(D(2024, 2, 29), Deadlines.BusinessDaysAfter(D(2024, 2, 28), 1,
                D(2024, 2, 1), D(2024, 3, 2), Weekend, None));
        }

        [Theory]
        [InlineData(2026, 1, 31, 1, 2026, 2, 28)]
        [InlineData(2024, 1, 31, 1, 2024, 2, 29)]
        [InlineData(2026, 1, 31, 3, 2026, 4, 30)]
        [InlineData(2024, 2, 29, 12, 2025, 2, 28)]
        [InlineData(2026, 1, 31, 0, 2026, 1, 31)]
        public void CalendarMonthsAreNotFixedLengthDays(int y, int m, int d, int months,
            int ey, int em, int ed)
        {
            Assert.Equal(D(ey, em, ed), Deadlines.CalendarMonthsAfter(D(y, m, d), months));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3661)]
        public void RejectsInvalidBusinessDayCounts(int days)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Calculate(D(2026, 1, 2), days));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(121)]
        public void RejectsInvalidMonthCounts(int months)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Deadlines.CalendarMonthsAfter(D(2026, 1, 1), months));
        }

        [Fact]
        public void RejectsNullConfigurationRatherThanDefaulting()
        {
            Assert.Throws<ArgumentNullException>(() => Deadlines.BusinessDaysAfter(D(2026, 1, 1), 1,
                D(2026, 1, 1), D(2026, 2, 1), null!, None));
            Assert.Throws<ArgumentNullException>(() => Deadlines.BusinessDaysAfter(D(2026, 1, 1), 1,
                D(2026, 1, 1), D(2026, 2, 1), Weekend, null!));
        }

        [Fact]
        public void RejectsImplicitTimezoneOrTimeTruncation()
        {
            foreach (DateTime value in new[] { DateTime.SpecifyKind(D(2026, 1, 2), DateTimeKind.Utc),
                DateTime.SpecifyKind(D(2026, 1, 2), DateTimeKind.Local), D(2026, 1, 2).AddHours(12) })
            {
                Assert.Throws<ArgumentException>(() => Calculate(value, 1));
                Assert.Throws<ArgumentException>(() => Deadlines.CalendarMonthsAfter(value, 1));
            }
        }

        [Fact]
        public void RejectsEmptyOrUnboundedCoverage()
        {
            Assert.Throws<ArgumentException>(() => Deadlines.BusinessDaysAfter(D(2026, 1, 2), 1,
                D(2026, 1, 1), D(2026, 1, 1), Weekend, None));
            Assert.Throws<ArgumentException>(() => Deadlines.BusinessDaysAfter(D(2026, 1, 2), 1,
                D(2026, 1, 1), D(2037, 1, 1), Weekend, None));
        }

        [Theory]
        [InlineData(2025, 12, 31)]
        [InlineData(2026, 2, 1)]
        public void RejectsStartOutsideCoverage(int y, int m, int d)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Calculate(D(y, m, d), 1));
        }

        [Fact]
        public void RejectsInvalidWeekdayDefinitions()
        {
            foreach (DayOfWeek[] days in new[] { new[] { (DayOfWeek)(-1) }, new[] { (DayOfWeek)7 },
                new[] { DayOfWeek.Sunday, DayOfWeek.Sunday }, Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().ToArray() })
            {
                Assert.Throws<ArgumentException>(() => Deadlines.BusinessDaysAfter(D(2026, 1, 2), 1,
                    D(2026, 1, 1), D(2026, 2, 1), days, None));
            }
        }

        [Fact]
        public void RejectsMalformedOrOversizedClosureLists()
        {
            foreach (DateTime[] dates in new[] { new[] { D(2025, 12, 31) }, new[] { D(2026, 2, 1) },
                new[] { D(2026, 1, 5), D(2026, 1, 5) }, new[] { D(2026, 1, 5).AddHours(1) },
                Enumerable.Repeat(D(2026, 1, 5), 3661).ToArray() })
            {
                Assert.Throws<ArgumentException>(() => Calculate(D(2026, 1, 2), 1, dates));
            }
        }

        [Fact]
        public void FailsWithoutGuessingBeyondTheKnownCalendar()
        {
            Assert.Throws<InvalidOperationException>(() => Calculate(D(2026, 1, 30), 1));
            Assert.Throws<InvalidOperationException>(() => Calculate(D(2026, 1, 2), 3660));
        }

        [Fact]
        public void RejectsUnrepresentableFutureMonth()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Deadlines.CalendarMonthsAfter(D(9999, 12, 31), 1));
        }
    }
}
