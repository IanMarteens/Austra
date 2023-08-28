namespace Austra.Library;

/// <summary>Implements a business days calendar and date calculator.</summary>
public sealed class Calendar
{
    /// <summary>Map of good business days.</summary>
    private readonly DateSet holidays;
    /// <summary>Fast check for empty holiday sets.</summary>
    private readonly bool hasHolidays;

    /// <summary>Initializes a business days calendar.</summary>
    /// <param name="name">Symbolic name of the calendar.</param>
    /// <param name="today">Current date.</param>
    /// <param name="holidays">A collection of dates.</param>
    public Calendar(string name, Date today, ICollection<Date> holidays)
    {
        this.holidays = new(holidays);
        hasHolidays = holidays.Count > 0;
        Name = name;
        Today = today;
        Tomorrow = RollFollowing(today + 1);
        TomorrowNext = RollFollowing(Tomorrow + 1);
    }

    /// <summary>Creates a new calendar by merging two existing ones.</summary>
    /// <param name="c1">First calendar to merge.</param>
    /// <param name="c2">Second calendar to merge.</param>
    public Calendar(Calendar c1, Calendar c2)
    {
        holidays = new(c1.holidays, c2.holidays);
        hasHolidays = holidays.Count > 0;
        Name = c1.Name + "+" + c2.Name;
        Today = c1.Today;
        Tomorrow = c1.Tomorrow;
        TomorrowNext = c1.TomorrowNext;
    }

    /// <summary>Creates a neutral calendar.</summary>
    /// <param name="today">Current date.</param>
    public Calendar(Date today) : this("NEUTRAL", today, Array.Empty<Date>()) { }

    /// <summary>Combines two calendars.</summary>
    /// <param name="c1">First calendar to merge.</param>
    /// <param name="c2">Second calendar to merge.</param>
    /// <returns>A new calendar with the union of all holidays.</returns>
    public static Calendar operator +(Calendar c1, Calendar c2) => new(c1, c2);

    /// <summary>Gets the name of the calendar.</summary>
    public string Name { get; }

    /// <summary>Gets the base or current date of the calendar.</summary>
    public Date Today { get; }

    /// <summary>Gets the good business day after today.</summary>
    public Date Tomorrow { get; }

    /// <summary>Gets the good business day after tomorrow.</summary>
    public Date TomorrowNext { get; }

    /// <summary>Is the given date a good business day or not?</summary>
    /// <param name="date">The date to be tested.</param>
    /// <returns><c>True</c> if the date is a holiday.</returns>
    public bool IsHoliday(Date date) => 
        ((uint)date + 2) % 7 <= 1 || holidays.Contains(date);

    /// <summary>Rolls a date up or down a number of working days.</summary>
    /// <param name="date">The date to be rolled.</param>
    /// <param name="days">A positive or negative number of working days.</param>
    /// <returns>The rolled date.</returns>
    public Date Roll(Date date, int days)
    {
        if (days > 0)
            do
            {
                date = RollFollowing(date + 1);
            }
            while (--days > 0);
        else if (days < 0)
            do
            {
                date = RollPrevious(date - 1);
            }
            while (++days < 0);
        return date;
    }

    /// <summary>Rolls a date up to the next business day.</summary>
    /// <param name="date">The date to be rolled.</param>
    /// <returns>The original date or the next business day.</returns>
    public Date RollFollowing(Date date)
    {
        while (true)
        {
            DayOfWeek dow = date.DayOfWeek;
            if (dow == DayOfWeek.Sunday)
            {
                date++;
                // Avoid re-testing the DOW, since it's a Monday.
                if (hasHolidays && holidays.Contains(date))
                    date++;
                else
                    return date;
            }
            else if (dow == DayOfWeek.Saturday)
            {
                date += 2;
                // Avoid re-testing the DOW, since it's a Monday.
                if (hasHolidays && holidays.Contains(date))
                    date++;
                else
                    return date;
            }
            else if (hasHolidays && holidays.Contains(date))
                if (dow == DayOfWeek.Friday)
                    date += 3;
                else
                    date++;
            else
                return date;
        }
    }

    /// <summary>Rolls a date down to the previous business day.</summary>
    /// <param name="date">The date to be rolled.</param>
    /// <returns>The original date or the previous business day.</returns>
    public Date RollPrevious(Date date)
    {
        while (true)
        {
            DayOfWeek dow = date.DayOfWeek;
            if (dow == DayOfWeek.Sunday)
            {
                date -= 2;
                // Avoid re-testing the DOW, since it's a Friday.
                if (holidays.Contains(date))
                    date--;
                else
                    return date;
            }
            else if (dow == DayOfWeek.Saturday)
                date--;
            else if (holidays.Contains(date))
                if (dow == DayOfWeek.Monday)
                    date -= 3;
                else
                    date--;
            else
                return date;
        }
    }

    /// <summary>Rolls a date up using the Modified Following convention.</summary>
    /// <param name="date">The date to be rolled.</param>
    /// <returns>The original date or the nearest business day in the month.</returns>
    public Date RollModifiedFollowing(Date date)
    {
        // A first roll-following step is factored out.
        // If the input date is valid, there's no need to check the 
        // change of month, which is an expensive test.
        DayOfWeek dow = date.DayOfWeek;
        Date d;
        if (dow == DayOfWeek.Sunday)
            d = date + 1;
        else if (dow == DayOfWeek.Saturday)
            d = date + 2;
        else if (hasHolidays && holidays.Contains(date))
            if (dow == DayOfWeek.Friday)
                d = date + 3;
            else
                d = date + 1;
        else
            return date;
        // Now, do a regular roll-following.
        d = RollFollowing(d);
        // Expensive month check and optional roll-previous.
        return d.Month != date.Month ? RollPrevious(date) : d;
    }

    /// <summary>Rolls a date up using the Modified Previous convention.</summary>
    /// <param name="date">The date to be rolled.</param>
    /// <returns>The original date or the nearest business day in the month.</returns>
    public Date RollModifiedPrevious(Date date)
    {
        Date d = RollPrevious(date);
        return d.Month != date.Month ? RollFollowing(date) : d;
    }

    /// <summary>Rolls a date given a business day convention.</summary>
    /// <param name="date">The date to be rolled.</param>
    /// <param name="businessDayConvention">The rolling algorithm.</param>
    /// <returns>The rolled date.</returns>
    public Date this[Date date, BDC businessDayConvention] => businessDayConvention switch
    {
        BDC.MODFOLLOWING => RollModifiedFollowing(date),
        BDC.FOLLOWING => RollFollowing(date),
        BDC.MODPREVIOUS => RollModifiedPrevious(date),
        BDC.PREVIOUS => RollPrevious(date),
        _ => date,
    };

    /// <summary>Gets a text representation of the calendar.</summary>
    /// <returns>The calendar's name.</returns>
    public override string ToString() => Name;

    /// <summary>Creates a time grid for future calculations.</summary>
    /// <param name="maturity">Last date in the sequence.</param>
    /// <param name="months">Number of months ahead. Use -1 as up to maturity.</param>
    /// <returns>An ordered sequence of dates.</returns>
    public IEnumerable<Date> GetTimeGrid(Date maturity, int months)
    {
        Date d0 = Today;
        if (months > 0 || Today.AddYears(1) >= maturity)
        {
            // This subalgorithm produces a daily grid.
            Date upTo = Today.AddMonths(months);
            if (upTo > maturity || months < 0)
                upTo = maturity;
            while (true)
            {
                d0 = RollFollowing(d0 + 1);
                if (d0 > upTo)
                    yield break;
                yield return d0;
            }
        }
        // Return a date a week later.
        Date d = RollModifiedFollowing(d0 + 7);
        if (d < maturity)
        {
            yield return d;
            // Return a date two weeks later.
            d = RollModifiedFollowing(d0 + 14);
            if (d < maturity)
            {
                yield return d;
                int m = 1;
                if (Today.AddYears(10) >= maturity)
                    while (true)
                    {
                        // Return two dates for every months.
                        Date date = RollModifiedFollowing(d0.AddMonths(m++));
                        if (date >= maturity)
                            break;
                        yield return date;
                        date = RollModifiedFollowing(date + 15);
                        if (date >= maturity)
                            break;
                        yield return date;
                    }
                else
                    while (true)
                    {
                        // Return a date for every month.
                        Date date = RollModifiedFollowing(d0.AddMonths(m++));
                        if (date >= maturity)
                            break;
                        yield return date;
                    }
            }
        }
        yield return maturity;
    }
}

/// <summary>Represents a business day convention.</summary>
public enum BDC
{
    /// <summary>Dates are not adjusted.</summary>
    NONE,
    /// <summary>
    /// Dates are adjusted to the next business day,
    /// except when a month boundary is crossed.
    /// </summary>
    MODFOLLOWING,
    /// <summary>Dates are always adjusted to the next business day.</summary>
    FOLLOWING,
    /// <summary>
    /// Dates are adjusted to the previous business day, 
    /// except when a month boundary is crossed.
    /// </summary>
    PREVIOUS,
    /// <summary>Dates are always adjusted to the previous business day.</summary>
    MODPREVIOUS,
}

/// <summary>Implements a date set using a very simple and fast hashtable.</summary>
internal sealed class DateSet
{
    /// <summary>The entries table always contains 2^N items.</summary>
    private Entry[] table;
    private int mask;
    private int threshold;

    /// <summary>Creates a date set with a given capacity.</summary>
    /// <param name="capacity">The desired capacity.</param>
    public DateSet(int capacity)
    {
        int cap = 16;
        while (cap < capacity)
            cap <<= 1;
        threshold = (int)(cap * 0.75);
        table = new Entry[cap];
        mask = cap - 1;
    }

    /// <summary>Creates a date set from a collection of dates.</summary>
    /// <param name="dates">A collection of dates.</param>
    public DateSet(ICollection<Date> dates) : this(dates.Count)
    {
        foreach (Date d in dates)
            Add(d);
    }

    /// <summary>Creates a date set by merging two existing ones.</summary>
    /// <param name="set1">First set to merge.</param>
    /// <param name="set2">Second set to merge.</param>
    public DateSet(DateSet set1, DateSet set2)
    {
        int cap = Max(set1.table.Length, set2.table.Length);
        threshold = (int)(cap * 0.75);
        table = new Entry[cap];
        mask = cap - 1;
        for (int i = 0; i < set1.table.Length; i++)
            for (Entry e = set1.table[i]; e != null; e = e.Next)
                Add(e.Key);
        for (int i = 0; i < set2.table.Length; i++)
            for (Entry e = set2.table[i]; e != null; e = e.Next)
                Add(e.Key);
    }

    /// <summary>Gets the number of items in the map.</summary>
    public int Count { get; private set; }

    /// <summary>Determines whether this set contains the specified date.</summary>
    /// <param name="date">The date to locate in the set.</param>
    /// <returns><see langword="True"/> if the date is already stored.</returns>
    public bool Contains(Date date)
    {
        for (Entry e = table[date.GetHashCode() & mask]; e != null; e = e.Next)
            if (e.Key == date)
                return true;
        return false;
    }

    /// <summary>Adds a date to a set, if not already present.</summary>
    /// <param name="date">The date to be added.</param>
    public void Add(Date date)
    {
        int i = date.GetHashCode() & mask;
        for (Entry e = table[i]; e != null; e = e.Next)
            if (e.Key == date)
                return;
        table[i] = new(date, table[i]);
        if (Count++ > threshold)
            Resize(2 * table.Length);
    }

    private void Resize(int newCapacity)
    {
        Entry[] newTable = new Entry[newCapacity];
        foreach (Entry e in table)
        {
            for (Entry e1 = e; e1 != null;)
            {
                Entry next = e1.Next;
                int i = e1.Key.GetHashCode() & (newCapacity - 1);
                e1.Next = newTable[i];
                newTable[i] = e1;
                e1 = next;
            }
        }
        table = newTable;
        mask = newCapacity - 1;
        threshold = (int)Min(newCapacity * 0.75, 1 << 30);
    }

    private sealed class Entry
    {
        public Date Key { get; }
        public Entry Next { get; set; }

        public Entry(Date key, Entry next) => (Key, Next) = (key, next);
    }
}
