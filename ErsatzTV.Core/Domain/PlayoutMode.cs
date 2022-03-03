namespace ErsatzTV.Core.Domain;

public enum PlayoutMode
{
    /// <summary>
    ///     Play items one after the other until a fixed start item is encountered
    /// </summary>
    Flood = 1,

    /// <summary>
    ///     Play one item from the collection
    /// </summary>
    One = 2,

    /// <summary>
    ///     Play a variable number of items from the collection
    /// </summary>
    Multiple = 3,

    /// <summary>
    ///     Play however many items will fit in the specified duration
    /// </summary>
    Duration = 4
}