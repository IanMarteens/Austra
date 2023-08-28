namespace Austra.Library;

/// <summary>Represents a date with efficient operations.</summary>
[JsonConverter(typeof(Date2JsonConverter))]
public readonly struct Date :
    IEquatable<Date>, IComparable<Date>,
    IEqualityOperators<Date, Date, bool>,
    IComparisonOperators<Date, Date, bool>,
    IAdditionOperators<Date, int, Date>,
    ISubtractionOperators<Date, int, Date>,
    ISubtractionOperators<Date, Date, int>,
    IIncrementOperators<Date>,
    IDecrementOperators<Date>
{
    /// <summary>Number of 100ns ticks per millisecond.</summary>
    private const long TicksPerMillisecond = 10000;
    /// <summary>Number of 100ns ticks per second.</summary>
    private const long TicksPerSecond = TicksPerMillisecond * 1000;
    /// <summary>Number of 100ns ticks per minute.</summary>
    private const long TicksPerMinute = TicksPerSecond * 60;
    /// <summary>Number of 100ns ticks per hour.</summary>
    private const long TicksPerHour = TicksPerMinute * 60;

    /// <summary>Number of 100ns ticks per day.</summary>
    public const long TicksPerDay = TicksPerHour * 24;

    /// <summary>Number of days in a non-leap year.</summary>
    private const int DaysPerYear = 365;
    /// <summary>Number of days in 4 years.</summary>
    private const int DaysPer4Years = DaysPerYear * 4 + 1;
    /// <summary>Number of days in 100 years.</summary>
    private const int DaysPer100Years = DaysPer4Years * 25 - 1;
    /// <summary>Number of days in 400 years.</summary>
    private const int DaysPer400Years = DaysPer100Years * 4 + 1;

    private const uint EafMultiplier = (uint)(((1UL << 32) + DaysPer4Years - 1) / DaysPer4Years);
    private const uint EafDivider = EafMultiplier * 4;
    private const int March1BasedDayOfNewYear = 306;

    /// <summary>Days before month's first day in a non-leap year.</summary>
    private static ReadOnlySpan<uint> DaysToMonth365 => new uint[]
    {
        0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365
    };

    /// <summary>Days before month's first day in a leap year.</summary>
    private static ReadOnlySpan<uint> DaysToMonth366 => new uint[]
    {
        0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366
    };

    private static ReadOnlySpan<byte> DaysInMonth365 => new byte[]
    {
        31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
    };

    private static ReadOnlySpan<byte> DaysInMonth366 => new byte[]
    {
        31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
    };

    /// <summary>Number of days since Jan 1st, 1.</summary>
    private readonly uint date;

    /// <summary>Creates a Date instance given its three components.</summary>
    /// <remarks>This constructor doesn't perform any validation on its arguments.</remarks>
    /// <param name="year">The year component.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    public Date(int year, int month, int day)
    {
        ReadOnlySpan<uint> days = IsLeapYear(year) ? DaysToMonth366 : DaysToMonth365;
        date = DaysToYear((uint)year) + days[month - 1] + (uint)day - 1;
    }

    /// <summary>Creates a Date from a day offset.</summary>
    /// <param name="days">Number of days since Jan 1st, 1.</param>
    public Date(uint days) => date = days;

    /// <summary>Creates a Date instance for Jan 1st of the given year.</summary>
    /// <param name="year">The year of the date.</param>
    public static Date FromYear(int year) => new(DaysToYear((uint)year));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint DaysToYear(uint year)
    {
        uint y = year - 1, cent = y / 100;
        return y * (365 * 4 + 1) / 4 - cent + cent / 4;
    }

    /// <summary>Checks if a given year is a leap year.</summary>
    /// <param name="year">The year to check.</param>
    /// <returns>
    /// <see langword="true"/>, if it's a leap year; <see langword="false"/>, otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLeapYear(int year)
    {
        if ((year & 3) != 0) return false;
        if ((year & 15) == 0) return true;
        return year % 25 != 0;
    }

    /// <summary>Checks if the date belongs to a leap year.</summary>
    /// <returns>
    /// <see langword="true"/>, if it's a leap year; <see langword="false"/>, otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsLeap()
    {
        int year = Year;
        if ((year & 3) != 0) return false;
        if ((year & 15) == 0) return true;
        return year % 25 != 0;
    }

    /// <summary>Extracts all components from a date in a single call.</summary>
    /// <param name="year">The year of the date.</param>
    /// <param name="month">The month of the date.</param>
    /// <param name="day">The day of month of the date.</param>
    public void Deconstruct(out int year, out int month, out int day)
    {
        // y100 = number of whole 100-year periods since 3/1/0000
        // r1 = (day number within 100-year period) * 4
        (uint y100, uint r1) = DivRem(((date * 4) | 3U) + 1224, DaysPer400Years);
        ulong u2 = (ulong)BigMul((int)EafMultiplier, (int)r1 | 3);
        ushort daySinceMarch1 = (ushort)((uint)u2 / EafDivider);
        int n3 = 2141 * daySinceMarch1 + 197913;
        year = (int)(100 * y100 + (uint)(u2 >> 32));
        // Compute month and day
        month = (ushort)(n3 >> 16);
        day = (ushort)n3 / 2141 + 1;
        // Rollover December 31
        if (daySinceMarch1 >= March1BasedDayOfNewYear)
        {
            ++year;
            month -= 12;
        }
    }

    /// <summary>Gets the day of week, being 0 the code for a Sunday.</summary>
    public DayOfWeek DayOfWeek => (DayOfWeek)((date + 1) % 7);

    /// <summary>Gets the year component from the date.</summary>
    public int Year
    {
        get
        {
            // y100 = number of whole 100-year periods since 1/1/0001
            // r1 = (day number within 100-year period) * 4
            (uint y100, uint r1) = DivRem(((date * 4) | 3U), DaysPer400Years);
            return 1 + (int)(100 * y100 + (r1 | 3) / DaysPer4Years);
        }
    }

    /// <summary>Gets the month component from the date.</summary>
    public int Month
    {
        get
        {
            // r1 = (day number within 100-year period) * 4
            uint r1 = (((date * 4) | 3U) + 1224) % DaysPer400Years;
            ulong u2 = (ulong)BigMul((int)EafMultiplier, (int)r1 | 3);
            ushort daySinceMarch1 = (ushort)((uint)u2 / EafDivider);
            int n3 = 2141 * daySinceMarch1 + 197913;
            return (ushort)(n3 >> 16) - (daySinceMarch1 >= March1BasedDayOfNewYear ? 12 : 0);
        }
    }

    /// <summary>Gets the day component from the date.</summary>
    public int Day
    {
        get
        {
            // r1 = (day number within 100-year period) * 4
            uint r1 = (((date * 4) | 3U) + 1224) % DaysPer400Years;
            ulong u2 = (ulong)BigMul((int)EafMultiplier, (int)r1 | 3);
            ushort daySinceMarch1 = (ushort)((uint)u2 / EafDivider);
            int n3 = 2141 * daySinceMarch1 + 197913;
            // Return 1-based day-of-month
            return (ushort)n3 / 2141 + 1;
        }
    }

    /// <summary>Adds a number of months to this date.</summary>
    /// <param name="months">Number of months to be added.</param>
    /// <returns>A new date several months ahead or before.</returns>
    public Date AddMonths(int months)
    {
        var (year, month, day) = this;
        int y = year, d = day;
        int m = month + months;
        int q = m > 0 ? (int)((uint)(m - 1) / 12) : m / 12 - 1;
        y += q;
        m -= q * 12;
        ReadOnlySpan<uint> daysTo = IsLeapYear(y) ? DaysToMonth366 : DaysToMonth365;
        uint daysToMonth = daysTo[m - 1];
        int days = (int)(daysTo[m] - daysToMonth);
        if (d > days) d = days;
        return new(DaysToYear((uint)y) + daysToMonth + (uint)d - 1);
    }

    /// <summary>Adds a number of years to this date.</summary>
    /// <param name="years">Number of years to be added.</param>
    /// <returns>A new date several years ahead or before.</returns>
    public Date AddYears(int years)
    {
        var (year, month, day) = this;
        int y = year + years;
        uint n = DaysToYear((uint)y);

        int m = month - 1, d = day - 1;
        if (IsLeapYear(y))
            n += DaysToMonth366[m];
        else
        {
            if (d == 28 && m == 1)
                d--;
            n += DaysToMonth365[m];
        }
        return new(n + (uint)d);
    }

    /// <summary>Change the day of a date to the first day in the month.</summary>
    /// <returns>The new truncated date.</returns>
    public Date TruncateDay() => new(date - (uint)Day + 1);

    /// <summary>Rolls this date up to the end of month.</summary>
    /// <returns>The last date in the date's month.</returns>
    public Date RollEOM()
    {
        var (y, m, d) = this;
        return this + (DaysInMonth(y, m) - d);
    }

    /// <summary>Rolls this date up to the third Wednesday of the month.</summary>
    /// <returns>The third Wednesday of the date's month.</returns>
    public Date RollIMM()
    {
        int d = Day;
        Date date = this - (d - 1);
        d = date.DayOfWeek - DayOfWeek.Wednesday;
        return d <= 0 ? date + (14 - d) : date + (21 - d);
    }

    /// <summary>Returns a hash code for this date.</summary>
    /// <returns>The internal integer representation of the date.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => (int)date;

    /// <summary>Compares this date to another object for equality.</summary>
    /// <param name="value">The other object.</param>
    /// <returns>
    /// <see langword="true"/> if the other object is the same date as the instance.
    /// </returns>
    public override bool Equals(object? value) => value is Date d && date == d.date;

    /// <summary>Compares this date to another date.</summary>
    /// <param name="value">The second date.</param>
    /// <returns><see langword="true"/> when they represent the same date.</returns>
    public bool Equals(Date value) => date == value.date;

    /// <summary>Adds a number of days to a date.</summary>
    /// <param name="d">The base date.</param>
    /// <param name="days">Days to be added.</param>
    /// <returns>A date past a number of days.</returns>
    public static Date operator +(Date d, int days) => new((uint)(d.date + days));
    /// <summary>Subtracts a number of days from a date.</summary>
    /// <param name="d">The base date.</param>
    /// <param name="days">Days to be subtracted.</param>
    /// <returns>A date before a number of days.</returns>
    public static Date operator -(Date d, int days) => new((uint)(d.date - days));
    /// <summary>Gets the number of days between two dates.</summary>
    /// <param name="d1">First date.</param>
    /// <param name="d2">Second date.</param>
    /// <returns>The number of days between the arguments.</returns>
    public static int operator -(Date d1, Date d2) => (int)(d1.date - d2.date);

    /// <summary>Compares the current date with another.</summary>
    /// <param name="other">A date to compare with this instance.</param>
    /// <returns>A value that indicates the relative order of the dates.</returns>
    public int CompareTo(Date other) => (int)(date - other.date);

    /// <summary>Compares two dates for equality.</summary>
    public static bool operator ==(Date d1, Date d2) => d1.date == d2.date;
    /// <summary>Compares two dates for inequality.</summary>
    public static bool operator !=(Date d1, Date d2) => d1.date != d2.date;
    /// <inheritdoc/>
    public static bool operator <(Date d1, Date d2) => d1.date < d2.date;
    /// <inheritdoc/>
    public static bool operator <=(Date d1, Date d2) => d1.date <= d2.date;
    /// <inheritdoc/>
    public static bool operator >(Date d1, Date d2) => d1.date > d2.date;
    /// <inheritdoc/>
    public static bool operator >=(Date d1, Date d2) => d1.date >= d2.date;

    /// <summary>Increments a date by a day.</summary>
    public static Date operator ++(Date d) => new(d.date + 1);
    /// <summary>Decrements a date by a day.</summary>
    public static Date operator --(Date d) => new(d.date - 1);

    /// <summary>Converts a date into a date and time.</summary>
    public static explicit operator DateTime(Date d) => new(d.date * TicksPerDay);
    /// <summary>Converts a date and time into a date.</summary>
    public static explicit operator Date(DateTime d) => new((uint)(d.Ticks / TicksPerDay));
    /// <summary>Converts a date to an unsigned integer.</summary>
    public static explicit operator uint(Date d) => d.date;

    /// <summary>Gets the maximum of two dates.</summary>
    public static Date Max(Date d1, Date d2) => d1.date >= d2.date ? d1 : d2;
    /// <summary>Gets the minimum of two dates.</summary>
    public static Date Min(Date d1, Date d2) => d1.date <= d2.date ? d1 : d2;

    /// <summary>Gets a textual representation of this date.</summary>
    /// <returns>The short date representation of this date.</returns>
    public override string ToString() => ((DateTime)this).ToShortDateString();

    /// <summary>Gets a textual representation of this date.</summary>
    /// <param name="format">A standard or custom date format.</param>
    /// <param name="formatProvider">Culture-specific formatting information.</param>
    /// <returns>The short date representation of this date.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        ((DateTime)this).ToString(format, formatProvider);

    /// <summary>Returns the number of days in the month given by a year and month.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month to check.</param>
    /// <returns>The number of days in the month for that year.</returns>
    public static int DaysInMonth(int year, int month) =>
        (IsLeapYear(year) ? DaysInMonth366 : DaysInMonth365)[month - 1];

    /// <summary>Adds a number of months to a given date.</summary>
    /// <param name="year">The year component.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="months">Number of months to add.</param>
    /// <returns>A date, several months ahead or before.</returns>
    public static Date AddMonths(int year, int month, int day, int months)
    {
        int i = month - 1 + months;
        return i >= 0 ?
            new(year + i / 12, i % 12 + 1, day) :
            new(year + (i - 11) / 12, 12 + (i + 1) % 12, day);
    }

    /// <summary>Gets the zero date.</summary>
    public static Date Zero { get; } = new(0);

    /// <summary>Gets the current date.</summary>
    public static Date Today => (Date)DateTime.Today;
}

/// <summary>Handles dates as integer values in JSON.</summary>
public class Date2JsonConverter : JsonConverter<Date>
{
    /// <inheritdoc/>
    public override Date Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        new(reader.GetUInt32());

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer,
        Date value,
        JsonSerializerOptions options) =>
        writer.WriteNumberValue((uint)value);
}
