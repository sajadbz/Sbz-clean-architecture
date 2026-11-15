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

    public async Task<TaskWriteResult> AddAsync(TaskItem item, CancellationToken cancellationToken)
    {
        db.Tasks.Add(item);
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

    public async Task<TaskWriteResult> RenameAsync(int id, TaskItem title, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await db.Tasks.Where(task => task.Id == id).ExecuteUpdateAsync(
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

    public async Task<bool> SetCompletedAsync(int id, bool completed, CancellationToken cancellationToken)
    {
        return await db.Tasks.Where(task => task.Id == id).ExecuteUpdateAsync(
            setters => setters.SetProperty(task => task.IsCompleted, completed), cancellationToken) != 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        => await db.Tasks.Where(task => task.Id == id).ExecuteDeleteAsync(cancellationToken) != 0;

    public Task<TaskSummary> FindAsync(int id, CancellationToken cancellationToken)
        => Project(db.Tasks.Where(task => task.Id == id)).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskSummary>> ListAsync(bool? completed, CancellationToken cancellationToken)
    {
        var tasks = completed.HasValue ? db.Tasks.Where(task => task.IsCompleted == completed.Value) : db.Tasks;
        return await Project(tasks.OrderBy(task => task.Id)).ToListAsync(cancellationToken);
    }

    private static IQueryable<TaskSummary> Project(IQueryable<TaskItem> tasks)
        => tasks.Select(task => new TaskSummary(task.Id, task.Title, task.IsCompleted));
}
