public enum RecurrenceType
{
    Minutely,
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Yearly
}

public class ScheduleInput
{
    public DateTime StartDateTime { get; set; }
    public RecurrenceType RecurrenceType { get; set; }
    public int Interval { get; set; }
    public int? DayOfMonth { get; set; }
    public int? Month { get; set; }
    public List<DayOfWeek>? DaysOfWeek { get; set; }
    public int? WeekOfMonth { get; set; }
    public DayOfWeek? DayOfWeekInMonth { get; set; }
}

public static DateTime GetNextSchedule(ScheduleInput input, DateTime currentTimeUtc)
{
    var timeOfDay = input.StartDateTime.TimeOfDay;
    DateTime next;

    switch (input.RecurrenceType)
    {
        case RecurrenceType.Minutely:
            next = input.StartDateTime;
            while (next <= currentTimeUtc)
                next = next.AddMinutes(input.Interval);
            return next;

        case RecurrenceType.Hourly:
            next = input.StartDateTime;
            while (next <= currentTimeUtc)
                next = next.AddHours(input.Interval);
            return next;

        case RecurrenceType.Daily:
            next = input.StartDateTime;
            while (next <= currentTimeUtc)
                next = next.AddDays(input.Interval);
            return next;

        case RecurrenceType.Weekly:
            if (input.DaysOfWeek == null || !input.DaysOfWeek.Any())
                throw new ArgumentException("DaysOfWeek required for weekly recurrence");

            var baseDate = input.StartDateTime.Date;
            var daysSorted = input.DaysOfWeek.OrderBy(d => d).ToList();

            while (true)
            {
                foreach (var day in daysSorted)
                {
                    var candidate = baseDate.AddDays((7 + day - baseDate.DayOfWeek) % 7).Add(timeOfDay);
                    if (candidate > currentTimeUtc)
                        return candidate;
                }
                baseDate = baseDate.AddDays(7 * input.Interval);
            }

        case RecurrenceType.Monthly:
            if (input.DayOfMonth.HasValue)
            {
                var date = new DateTime(input.StartDateTime.Year, input.StartDateTime.Month, 1);
                while (true)
                {
                    var candidateDay = Math.Min(input.DayOfMonth.Value, DateTime.DaysInMonth(date.Year, date.Month));
                    var candidate = new DateTime(date.Year, date.Month, candidateDay).Add(timeOfDay);
                    if (candidate > currentTimeUtc)
                        return candidate;
                    date = date.AddMonths(input.Interval);
                }
            }
            else if (input.WeekOfMonth.HasValue && input.DayOfWeekInMonth.HasValue)
            {
                var date = new DateTime(input.StartDateTime.Year, input.StartDateTime.Month, 1);
                while (true)
                {
                    var candidate = GetNthWeekdayOfMonth(date.Year, date.Month, input.DayOfWeekInMonth.Value, input.WeekOfMonth.Value).Add(timeOfDay);
                    if (candidate > currentTimeUtc)
                        return candidate;
                    date = date.AddMonths(input.Interval);
                }
            }
            else
                throw new ArgumentException("Either DayOfMonth or (WeekOfMonth and DayOfWeekInMonth) must be provided");

        case RecurrenceType.Yearly:
            if (input.Month.HasValue && input.DayOfMonth.HasValue)
            {
                int year = input.StartDateTime.Year;
                while (true)
                {
                    var candidate = new DateTime(year, input.Month.Value, input.DayOfMonth.Value).Add(timeOfDay);
                    if (candidate > currentTimeUtc)
                        return candidate;
                    year += input.Interval;
                }
            }
            else if (input.Month.HasValue && input.WeekOfMonth.HasValue && input.DayOfWeekInMonth.HasValue)
            {
                int year = input.StartDateTime.Year;
                while (true)
                {
                    var candidate = GetNthWeekdayOfMonth(year, input.Month.Value, input.DayOfWeekInMonth.Value, input.WeekOfMonth.Value).Add(timeOfDay);
                    if (candidate > currentTimeUtc)
                        return candidate;
                    year += input.Interval;
                }
            }
            else
                throw new ArgumentException("Yearly recurrence requires Month and DayOfMonth or WeekOfMonth + DayOfWeekInMonth");

        default:
            throw new NotSupportedException("Unsupported recurrence type");
    }
}

private static DateTime GetNthWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek, int nth)
{
    var firstDay = new DateTime(year, month, 1);
    var daysOffset = ((int)dayOfWeek - (int)firstDay.DayOfWeek + 7) % 7;
    var day = 1 + daysOffset + 7 * (nth - 1);
    if (day > DateTime.DaysInMonth(year, month))
        throw new ArgumentException($"Month {month}/{year} doesn't have {nth} {dayOfWeek}s");
    return new DateTime(year, month, day);
}


//


public static void ValidateScheduleInput(ScheduleInput input)
{
    if (input.Interval <= 0)
        throw new ArgumentException("Interval must be greater than 0");

    if (input.Hour < 0 || input.Hour > 23)
        throw new ArgumentException("Hour must be between 0 and 23");

    if (input.Minute < 0 || input.Minute > 59)
        throw new ArgumentException("Minute must be between 0 and 59");

    switch (input.RecurrenceType)
    {
        case RecurrenceType.Weekly:
            if (input.DaysOfWeek == null || !input.DaysOfWeek.Any())
                throw new ArgumentException("Weekly recurrence requires at least one day of the week");
            break;

        case RecurrenceType.Monthly:
            if (input.DayOfMonth.HasValue && (input.WeekOfMonth.HasValue || input.DayOfWeekInMonth.HasValue))
                throw new ArgumentException("Monthly recurrence must specify either DayOfMonth or WeekOfMonth + DayOfWeekInMonth, not both");

            if (!input.DayOfMonth.HasValue && (!input.WeekOfMonth.HasValue || !input.DayOfWeekInMonth.HasValue))
                throw new ArgumentException("Monthly recurrence must have DayOfMonth or both WeekOfMonth and DayOfWeekInMonth");

            if (input.DayOfMonth.HasValue && (input.DayOfMonth < 1 || input.DayOfMonth > 31))
                throw new ArgumentException("DayOfMonth must be between 1 and 31");
            break;

        case RecurrenceType.Yearly:
            if (!input.Month.HasValue || input.Month < 1 || input.Month > 12)
                throw new ArgumentException("Yearly recurrence must include a valid Month (1-12)");

            bool hasDay = input.DayOfMonth.HasValue;
            bool hasWeekPattern = input.WeekOfMonth.HasValue && input.DayOfWeekInMonth.HasValue;

            if (!hasDay && !hasWeekPattern)
                throw new ArgumentException("Yearly recurrence must specify either DayOfMonth or WeekOfMonth + DayOfWeekInMonth");

            if (hasDay && hasWeekPattern)
                throw new ArgumentException("Specify either DayOfMonth or WeekOfMonth + DayOfWeekInMonth for yearly recurrence, not both");

            if (input.DayOfMonth.HasValue && (input.DayOfMonth < 1 || input.DayOfMonth > 31))
                throw new ArgumentException("DayOfMonth must be between 1 and 31");
            break;
    }
}

