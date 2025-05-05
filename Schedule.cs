using System;
using System.Globalization;

public static class NextScheduleCalculator
{
    public static DateTimeOffset? Calculate(Schedule schedule)
    {
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZone));
        var start = TimeZoneInfo.ConvertTime(schedule.StartDateTime, TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZone));
        if (schedule.EndDateTime.HasValue && now > schedule.EndDateTime.Value)
            return null;

        return schedule.RecurrenceType.ToLower() switch
        {
            "minutely" => NextMinutely(now, start, schedule.Interval),
            "hourly" => NextHourly(now, start, schedule.Interval),
            "daily" => NextDaily(now, start, schedule.Interval),
            "weekly" => NextWeekly(now, start, schedule),
            "monthly" => NextMonthly(now, start, schedule),
            "yearly" => NextYearly(now, start, schedule),
            _ => null
        };
    }

    private static DateTimeOffset NextMinutely(DateTimeOffset now, DateTimeOffset start, int interval)
    {
        var minutesSinceStart = (int)(now - start).TotalMinutes;
        var nextMinutes = ((minutesSinceStart / interval) + 1) * interval;
        return start.AddMinutes(nextMinutes);
    }

    private static DateTimeOffset NextHourly(DateTimeOffset now, DateTimeOffset start, int interval)
    {
        var hoursSinceStart = (int)(now - start).TotalHours;
        var nextHours = ((hoursSinceStart / interval) + 1) * interval;
        return start.AddHours(nextHours);
    }

    private static DateTimeOffset NextDaily(DateTimeOffset now, DateTimeOffset start, int interval)
    {
        var daysSinceStart = (int)(now.Date - start.Date).TotalDays;
        var nextDays = ((daysSinceStart / interval) + 1) * interval;
        return start.AddDays(nextDays);
    }

    private static DateTimeOffset NextWeekly(DateTimeOffset now, DateTimeOffset start, Schedule schedule)
    {
        var interval = schedule.Interval;
        var daysOfWeek = schedule.DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var currentWeekStart = start.AddDays(((int)(now.Date - start.Date).TotalDays / (interval * 7)) * (interval * 7));

        for (int i = 0; i < interval * 7; i++)
        {
            var candidate = currentWeekStart.AddDays(i);
            if (candidate > now && daysOfWeek.Contains(candidate.DayOfWeek.ToString(), StringComparer.OrdinalIgnoreCase))
                return candidate;
        }

        // Move to next recurrence week
        return NextWeekly(now.AddDays(7 * interval), start, schedule);
    }

    private static DateTimeOffset NextMonthly(DateTimeOffset now, DateTimeOffset start, Schedule schedule)
    {
        var candidate = start;
        while (candidate <= now)
        {
            candidate = candidate.AddMonths(schedule.Interval);
        }

        if (schedule.DayOfMonth.HasValue)
        {
            var day = Math.Min(schedule.DayOfMonth.Value, DateTime.DaysInMonth(candidate.Year, candidate.Month));
            return new DateTimeOffset(candidate.Year, candidate.Month, day, start.Hour, start.Minute, 0, start.Offset);
        }

        if (schedule.WeekOfMonth.HasValue && !string.IsNullOrWhiteSpace(schedule.DayOfWeekInMonth))
        {
            return GetNthDayOfWeekInMonth(candidate.Year, candidate.Month, schedule.DayOfWeekInMonth, schedule.WeekOfMonth.Value, start);
        }

        return candidate;
    }

    private static DateTimeOffset NextYearly(DateTimeOffset now, DateTimeOffset start, Schedule schedule)
    {
        var candidate = new DateTimeOffset(now.Year, schedule.MonthOfYear ?? 1, 1, start.Hour, start.Minute, 0, start.Offset);
        if (schedule.DayOfMonth.HasValue)
        {
            var day = Math.Min(schedule.DayOfMonth.Value, DateTime.DaysInMonth(candidate.Year, schedule.MonthOfYear ?? 1));
            candidate = new DateTimeOffset(candidate.Year, candidate.Month, day, start.Hour, start.Minute, 0, start.Offset);
            if (candidate <= now)
                candidate = candidate.AddYears(schedule.Interval);
            return candidate;
        }

        if (schedule.WeekOfMonth.HasValue && !string.IsNullOrWhiteSpace(schedule.DayOfWeekInMonth))
        {
            while (candidate <= now)
                candidate = candidate.AddYears(schedule.Interval);
            return GetNthDayOfWeekInMonth(candidate.Year, schedule.MonthOfYear ?? 1, schedule.DayOfWeekInMonth, schedule.WeekOfMonth.Value, start);
        }

        return candidate;
    }

    private static DateTimeOffset GetNthDayOfWeekInMonth(int year, int month, string dayOfWeekStr, int weekOfMonth, DateTimeOffset baseTime)
    {
        if (!Enum.TryParse<DayOfWeek>(dayOfWeekStr, true, out var dayOfWeek))
            throw new ArgumentException("Invalid day of week");

        var firstDay = new DateTime(year, month, 1);
        var daysToAdd = ((int)dayOfWeek - (int)firstDay.DayOfWeek + 7) % 7;
        var resultDay = firstDay.AddDays(daysToAdd + (weekOfMonth - 1) * 7);

        // Handle last occurrence
        if (weekOfMonth == -1)
        {
            var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            var lastDOW = lastDay.DayOfWeek;
            var offset = ((int)lastDOW - (int)dayOfWeek + 7) % 7;
            resultDay = lastDay.AddDays(-offset);
        }

        return new DateTimeOffset(resultDay.Year, resultDay.Month, resultDay.Day, baseTime.Hour, baseTime.Minute, 0, baseTime.Offset);
    }
}
