namespace Austra.Library.MVO;

/// <summary>A set keeping its items in sorted order.</summary>
/// <param name="capacity">The maximum number of items in this set.</param>
internal struct CSet(int capacity)
{
    /// <summary>Stored items.</summary>
    private readonly int[] items = new int[capacity];

    /// <summary>Gets the number of items in this set.</summary>
    public int Count { readonly get; private set; }

    /// <summary>Gets an item by its index.</summary>
    /// <param name="index">The index.</param>
    /// <returns>The item.</returns>
    public readonly int this[int index] => items[index];

    public void RemoveAt(int index)
    {
        if (--Count >= index)
            Array.Copy(items, index + 1, items, index, Count - index);
    }

    /// <summary>Inserts the item into the set while keeping the order.</summary>
    /// <param name="newItem">The item to be added.</param>
    public void Add(int newItem)
    {
        int i;
        for (i = Count; i > 0; i--)
        {
            if (newItem > items[i - 1])
                break;
            items[i] = items[i - 1];
        }
        items[i] = newItem;
        Count++;
    }

    /// <summary>Deletes the given value from the set.</summary>
    /// <param name="item">Value to be deleted.</param>
    public void Remove(int item) => RemoveAt(Find(item));

    /// <summary>Locates an item in the array.</summary>
    /// <param name="item">Value to be located.</param>
    /// <returns>The position in the array.</returns>
    public readonly int Find(int item)
    {
        for (int i = 0; i < Count; i++)
            if (items[i] == item)
                return i;
        return -1;
    }

    /// <summary>Gets a textual representation of this set.</summary>
    /// <returns>The list of stored items.</returns>
    public override readonly string ToString()
    {
        StringBuilder s = new('[');
        if (Count > 0)
        {
            s.Append(items[0]);
            for (int i = 1; i < Count; i++)
                s.Append(' ').Append(items[i]);
        }
        return s.Append(']').ToString();
    }
}
