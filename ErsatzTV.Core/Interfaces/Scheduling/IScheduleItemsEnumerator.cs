using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IScheduleItemsEnumerator
{
    CollectionEnumeratorState State { get; }
    ProgramScheduleItem Current { get; }
    void MoveNext();
    ProgramScheduleItem Peek(int offset);
}
