namespace ErsatzTV.Core.Scheduling;

public enum PlayoutBuildMode
{
    /// <summary>
    ///     Continue building the playout into the future, without changing any existing playout items
    /// </summary>
    Continue = 1,

    /// <summary>
    ///     Rebuild the playout while attempting to maintain collection progress
    /// </summary>
    Refresh = 2,

    /// <summary>
    ///     Rebuild the playout from scratch (clearing all state)
    /// </summary>
    Reset = 3
}
