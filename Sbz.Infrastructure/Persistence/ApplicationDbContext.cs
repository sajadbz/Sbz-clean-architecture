using Microsoft.EntityFrameworkCore;
using Sbz.Domain.Tasks;

namespace Sbz.Infrastructure.Persistence;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var tasks = modelBuilder.Entity<TaskItem>();
        tasks.Property(task => task.Title).IsRequired().HasMaxLength(TaskItem.MaxTitleLength);
        tasks.Property(task => task.NormalizedTitle).IsRequired();
        tasks.HasIndex(task => task.NormalizedTitle).IsUnique();
    }
}
