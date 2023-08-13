namespace ErsatzTV.Scanner;

public record Provider(string Name, string Assembly) 
{
    public static Provider Sqlite = new (nameof(Sqlite), typeof(Infrastructure.Sqlite.Marker).Assembly.GetName().Name!);
    public static Provider MySql = new (nameof(MySql), typeof(Infrastructure.MySql.Marker).Assembly.GetName().Name!);
}