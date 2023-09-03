namespace ErsatzTV;

public record Provider(string Name, string Assembly) 
{
    public static readonly Provider Sqlite = new (nameof(Sqlite), typeof(Infrastructure.Sqlite.Marker).Assembly.GetName().Name!);
    public static readonly Provider MySql = new (nameof(MySql), typeof(Infrastructure.MySql.Marker).Assembly.GetName().Name!);
}