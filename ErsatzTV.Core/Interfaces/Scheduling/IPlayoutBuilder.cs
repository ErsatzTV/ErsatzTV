using System;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Scheduling
{
    public interface IPlayoutBuilder
    {
        public Task<Playout> BuildPlayoutItems(Playout playout, bool rebuild = false);

        public Task<Playout> BuildPlayoutItems(
            Playout playout,
            DateTimeOffset playoutStart,
            DateTimeOffset playoutFinish,
            bool rebuild = false);
    }
}
