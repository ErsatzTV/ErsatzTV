namespace ErsatzTV.Core.Interfaces.Database;

public interface IDatabaseMigrations
{
    Task<IReadOnlyList<string>> GetUnknownMigrations();
}
