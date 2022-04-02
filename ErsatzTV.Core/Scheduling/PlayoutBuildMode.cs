namespace ErsatzTV.Core.Scheduling;

public enum PlayoutBuildMode
{
    // this continues building playout into the future
    Continue = 1,

    // this rebuilds a playout but will maintain collection progress
    Refresh = 2,
    
    // this rebuilds a playout and clears all state
    Reset = 3
}
