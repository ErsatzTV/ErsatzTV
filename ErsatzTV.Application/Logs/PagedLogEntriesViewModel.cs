namespace ErsatzTV.Application.Logs;

public record PagedLogEntriesViewModel(int TotalCount, List<LogEntryViewModel> Page);
