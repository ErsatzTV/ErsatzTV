using System;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Scheduling
{
    public interface IPlayoutBuilder
    {
        Task<Playout> BuildPlayoutItems(Playout playout, bool rebuild = false);

        Task<Playout> BuildPlayoutItems(
            Playout playout,
            DateTimeOffset playoutStart,
            DateTimeOffset playoutFinish,
            bool rebuild = false);
    }
}
