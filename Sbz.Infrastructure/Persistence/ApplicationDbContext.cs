using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sbz.Domain.Tasks;

namespace Sbz.Infrastructure.Persistence;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var tasks = modelBuilder.Entity<TaskItem>();
        tasks.Property(task => task.Title).IsRequired().HasMaxLength(TaskItem.MaxTitleLength);
        tasks.Property(task => task.NormalizedTitle).IsRequired();
        tasks.HasIndex(task => new { task.OwnerId, task.NormalizedTitle }).IsUnique();
        tasks.HasOne<IdentityUser>().WithMany().HasForeignKey(task => task.OwnerId).OnDelete(DeleteBehavior.SetNull);
    }
}
