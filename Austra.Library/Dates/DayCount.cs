namespace Austra.Library;

/// <summary>Implements algorithms for calculating year fractions.</summary>
public static class DayCount
{
    /// <summary>Represents the ACT/360 day count convention.</summary>
    public const int Act360 = 0;
    /// <summary>Represents the ACT/365 day count convention.</summary>
    public const int Act365 = 1;
    /// <summary>Represents the 30/360 day count convention.</summary>
    public const int D30x360 = 2;
    /// <summary>Represents the 30E/360 day count convention.</summary>
    public const int D30Ex360 = 3;
    /// <summary>Represents the 30E/360.ISDA day count convention.</summary>
    public const int D30Ex360ISDA = 4;
    /// <summary>Represents the ACT/ACT day count convention.</summary>
    public const int ActAct = 5;
    /// <summary>Represents the ACT/ACT.ICMA day count convention.</summary>
    public const int ActActICMA = 6;
    /// <summary>Represents an unknown day count convention.</summary>
    public const int UNKNOWN = -1;

    /// <summary>Implements the ACT/365 day count convention.</summary>
    /// <param name="from">First date of the period.</param>
    /// <param name="to">Second date of the period.</param>
    /// <returns>The number of days divided by 365.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetAccrual365(Date from, Date to) => (to - from) / 365.0;

    /// <summary>Implements the ACT/360 day count convention.</summary>
    /// <param name="from">First date of the period.</param>
    /// <param name="to">Second date of the period.</param>
    /// <returns>The number of days divided by 360.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetAccrual360(Date from, Date to) => (to - from) / 360.0;

    /// <summary>Implements the ACT/ACT.ICMA day count convention.</summary>
    /// <param name="from">First date of the period.</param>
    /// <param name="to">Second date of the period.</param>
    /// <param name="period">Calculation period, in months.</param>
    /// <param name="isStub">Is this interval a stub.</param>
    /// <param name="isFront">Is this an initial stub.</param>
    /// <param name="isShort">Is this a short stub.</param>
    /// <returns>The year fraction.</returns>
    public static double GetAccrualActICMA(
        Date from,
        Date to,
        int period,
        bool isStub, bool isFront, bool isShort)
    {
        if (!isStub)
            return period / 12.0;
        else if (isShort)
            return isFront ?
                period * (to - from) / (12.0 * (to - to.AddMonths(-period))) :
                period * (to - from) / (12.0 * (from.AddMonths(period) - from));
        else if (isFront)
        {
            Date mid = to.AddMonths(-period);
            return period / 12.0 * (1 + (double)(mid - from) / (mid - mid.AddMonths(-period)));
        }
        else
        {
            Date mid = from.AddMonths(period);
            return period / 12.0 * (1 + (double)(to - mid) / (mid.AddMonths(period) - mid));
        }
    }

    /// <summary>Implements the ACT/ACT day count convention.</summary>
    /// <param name="from">First date of the period.</param>
    /// <param name="to">Second date of the period.</param>
    /// <returns>The year fraction.</returns>
    public static double GetAccrualActual(Date from, Date to)
    {
        int y1 = from.Year, y2 = to.Year;
        if (y1 == y2)
            return Date.IsLeapYear(y1) ?
                    (to - from) / 366.0 :
                    (to - from) / 365.0;
        int leaps = 0, nonLeaps = 0, y = y1 + 1;
        Date d = Date.FromYear(y);
        if (Date.IsLeapYear(y1))
            leaps += d - from;
        else
            nonLeaps += d - from;
        while (d < to)
        {
            if (y == y2)
            {
                if (Date.IsLeapYear(y))
                    leaps += to - d;
                else
                    nonLeaps += to - d;
                break;
            }
            if (Date.IsLeapYear(y))
                leaps += 366;
            else
                nonLeaps += 365;
            d = Date.FromYear(++y);
        }
        return nonLeaps / 365.0 + leaps / 366.0;
    }

    /// <summary>Implements the 30/360 day count convention.</summary>
    /// <param name="from">First date of the period.</param>
    /// <param name="to">Second date of the period.</param>
    /// <returns>The year fraction.</returns>
    public static double GetAccrual30_360(Date from, Date to)
    {
        var (y1, m1, day1) = from;
        var (y2, m2, day2) = to;
        if (day1 == 31)
            day1 = 30;
        if (day2 == 31 && day1 == 30)
            day2 = 30;
        return (y2 - y1) + ((m2 - m1) * 30 + day2 - day1) / 360.0;
    }

    /// <summary>Implements the 30E/360 day count convention.</summary>
    /// <param name="from">First date of the period.</param>
    /// <param name="to">Second date of the period.</param>
    /// <returns>The year fraction.</returns>
    public static double GetAccrual30E360(Date from, Date to)
    {
        var (y1, m1, day1) = from;
        var (y2, m2, day2) = to;
        if (day1 == 31)
            day1 = 30;
        if (day2 == 31)
            day2 = 30;
        return (y2 - y1) + ((m2 - m1) * 30 + day2 - day1) / 360.0;
    }

    /// <summary>Implements the 30E/360.ISDA day count convention.</summary>
    /// <param name="from">First date of the period.</param>
    /// <param name="to">Second date of the period.</param>
    /// <param name="isLast">Is this the last period of the schedule?</param>
    /// <returns>The year fraction.</returns>
    public static double GetAccrual30E360ISDA(Date from, Date to, bool isLast)
    {
        var (y1, m1, day1) = from;
        var (y2, m2, day2) = to;
        if (day1 == Date.DaysInMonth(y1, m1))
            day1 = 30;
        if (day2 == 31 ||
            m2 == 2 && !isLast && day2 == Date.DaysInMonth(y2, m2))
            day2 = 30;
        return (y2 - y1) + ((m2 - m1) * 30 + day2 - day1) / 360.0;
    }

    /// <summary>
    /// Translates a day coount convention string into an integer constant,
    /// taking some common synonyms into account.
    /// </summary>
    /// <param name="dayCount">The day count identifier un upper-case.</param>
    /// <returns>An integer constant, or UNKNOWN if not found.</returns>
    public static int Translate(string dayCount) => dayCount switch
    {
        "30/360" => D30x360,
        "30E/360" => D30Ex360,
        "30E/360.ISDA" or "30E/360 ISDA" => D30Ex360ISDA,
        "ACT/ACT" or "ACT/ACT.ISDA" or "ACT/ACT ISDA" or "ACT/365.ISDA" or "ACT/365 ISDA"
            or "ACTUAL/ACTUAL" or "ACTUAL/ACTUAL.ISDA" or "ACTUAL/ACTUAL ISDA"
            or "ACTUAL/365.ISDA" or "ACTUAL/365 ISDA" => ActAct,
        "ACT/365" or "ACT/365.FIXED" or "ACT/365 FIXED" or "ACTUAL/365"
            or "ACTUAL/365.FIXED" or "ACTUAL/365 FIXED" => Act365,
        "ACT/ACT.ICMA" or "ACT/ACT ICMA" or "ACT/ACT.ISMA" or "ACT/ACT ISMA"
            or "ACTUAL/ACTUAL.ICMA" or "ACTUAL/ACTUAL ICMA" or "ACTUAL/ACTUAL.ISMA"
            or "ACTUAL/ACTUAL ISMA" => ActActICMA,
        "ACT/360" or "ACTUAL/360" => Act360,
        _ => UNKNOWN,
    };
}
