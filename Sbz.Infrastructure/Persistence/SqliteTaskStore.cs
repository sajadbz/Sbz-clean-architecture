using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sbz.Application.Tasks;
using Sbz.Domain.Tasks;

namespace Sbz.Infrastructure.Persistence;

internal sealed class SqliteTaskStore(ApplicationDbContext db) : ITaskStore
{
    private const int SqliteUniqueConstraint = 2067;

    public async Task<TaskWriteResult> AddAsync(string ownerId, TaskItem item, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        db.Tasks.Add(item);
        db.Entry(item).Property(task => task.OwnerId).CurrentValue = ownerId;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return TaskWriteResult.Saved;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraint })
        {
            db.Entry(item).State = EntityState.Detached;
            return TaskWriteResult.DuplicateTitle;
        }
    }

    public async Task<TaskWriteResult> RenameAsync(string ownerId, int id, TaskItem title, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await OwnedBy(ownerId).Where(task => task.Id == id).ExecuteUpdateAsync(
                setters => setters.SetProperty(task => task.Title, title.Title)
                                  .SetProperty(task => task.NormalizedTitle, title.NormalizedTitle),
                cancellationToken);
            return updated == 0 ? TaskWriteResult.NotFound : TaskWriteResult.Saved;
        }
        catch (SqliteException exception) when (exception.SqliteExtendedErrorCode == SqliteUniqueConstraint)
        {
            return TaskWriteResult.DuplicateTitle;
        }
    }

    public async Task<bool> SetCompletedAsync(string ownerId, int id, bool completed, CancellationToken cancellationToken)
    {
        return await OwnedBy(ownerId).Where(task => task.Id == id).ExecuteUpdateAsync(
            setters => setters.SetProperty(task => task.IsCompleted, completed), cancellationToken) != 0;
    }

    public async Task<bool> DeleteAsync(string ownerId, int id, CancellationToken cancellationToken)
        => await OwnedBy(ownerId).Where(task => task.Id == id).ExecuteDeleteAsync(cancellationToken) != 0;

    public Task<TaskSummary> FindAsync(string ownerId, int id, CancellationToken cancellationToken)
        => Project(OwnedBy(ownerId).Where(task => task.Id == id)).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskSummary>> ListAsync(string ownerId, bool? completed, CancellationToken cancellationToken)
    {
        var tasks = OwnedBy(ownerId);
        if (completed.HasValue)
            tasks = tasks.Where(task => task.IsCompleted == completed.Value);
        return await Project(tasks.OrderBy(task => task.Id)).ToListAsync(cancellationToken);
    }

    private IQueryable<TaskItem> OwnedBy(string ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        return db.Tasks.Where(task => task.OwnerId == ownerId);
    }

    private static IQueryable<TaskSummary> Project(IQueryable<TaskItem> tasks)
        => tasks.Select(task => new TaskSummary(task.Id, task.Title, task.IsCompleted));
}
