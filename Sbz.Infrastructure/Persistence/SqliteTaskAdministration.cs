using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sbz.Application.Tasks;
using Sbz.Domain.Tasks;

namespace Sbz.Infrastructure.Persistence;

internal sealed class SqliteTaskAdministration(ApplicationDbContext db, UserManager<IdentityUser> users)
    : ITaskAdministration
{
    private const int PageSize = 25;

    public async Task<ManagedTaskPage> ListAsync(string search, bool unassigned, int page, CancellationToken cancellationToken)
    {
        var tasks = db.Tasks.AsNoTracking();
        if (unassigned)
            tasks = tasks.Where(task => task.OwnerId == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();
            tasks = tasks.Where(task => task.NormalizedTitle.Contains(normalized));
        }

        var total = await tasks.CountAsync(cancellationToken);
        var items = await Project(tasks.OrderBy(task => task.Id).Skip((page - 1) * PageSize).Take(PageSize)).ToListAsync(cancellationToken);
        return new ManagedTaskPage(items, total);
    }

    public Task<ManagedTask> FindAsync(int id, CancellationToken cancellationToken)
        => Project(db.Tasks.Where(task => task.Id == id)).SingleOrDefaultAsync(cancellationToken);

    public async Task<ManagedTaskResult> CreateAsync(
        string title, bool completed, string ownerName, CancellationToken cancellationToken)
    {
        var task = new TaskItem(title);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var owner = await FindOwnerAsync(ownerName);
        if (owner is null && !string.IsNullOrWhiteSpace(ownerName))
            return ManagedTaskResult.InvalidOwner;

        var entry = db.Tasks.Add(task);
        entry.Property(item => item.OwnerId).CurrentValue = owner?.Id;
        entry.Property(item => item.IsCompleted).CurrentValue = completed;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ManagedTaskResult.Saved;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        {
            entry.State = EntityState.Detached;
            return ManagedTaskResult.DuplicateTitle;
        }
    }

    public async Task<ManagedTaskResult> UpdateAsync(
        int id, string title, bool completed, string ownerName, CancellationToken cancellationToken)
    {
        var validated = new TaskItem(title);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var owner = await FindOwnerAsync(ownerName);
        if (owner is null && !string.IsNullOrWhiteSpace(ownerName))
            return ManagedTaskResult.InvalidOwner;
        var ownerId = owner?.Id;

        try
        {
            var updated = await db.Tasks.Where(task => task.Id == id).ExecuteUpdateAsync(
                setters => setters.SetProperty(task => task.Title, validated.Title)
                    .SetProperty(task => task.NormalizedTitle, validated.NormalizedTitle)
                    .SetProperty(task => task.IsCompleted, completed)
                    .SetProperty(task => task.OwnerId, ownerId),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return updated == 0 ? ManagedTaskResult.NotFound : ManagedTaskResult.Saved;
        }
        catch (SqliteException exception) when (exception.SqliteExtendedErrorCode == 2067)
        {
            return ManagedTaskResult.DuplicateTitle;
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        => await db.Tasks.Where(task => task.Id == id).ExecuteDeleteAsync(cancellationToken) != 0;

    private Task<IdentityUser> FindOwnerAsync(string name)
        => string.IsNullOrWhiteSpace(name) ? Task.FromResult<IdentityUser>(null) : users.FindByNameAsync(name.Trim());

    private IQueryable<ManagedTask> Project(IQueryable<TaskItem> tasks)
        => from task in tasks
           join user in db.Users on task.OwnerId equals user.Id into owners
           from owner in owners.DefaultIfEmpty()
           select new ManagedTask(task.Id, task.Title, task.IsCompleted, task.OwnerId, owner.UserName);
}
