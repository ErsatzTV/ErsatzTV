using ErsatzTV.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data;

public class LogContext : DbContext
{
    public LogContext(DbContextOptions<LogContext> options)
        : base(options)
    {
    }

    public DbSet<LogEntry> LogEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<LogEntry>().ToTable("Logs");
    }
}
