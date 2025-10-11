namespace ErsatzTV.Application.Playouts;

public record PlayoutHistoryViewModel(int Id, DateTimeOffset When, DateTimeOffset Finish, string Key, string Details);
