using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sbz.Domain.Tasks;

namespace Sbz.Application.Tasks;

public sealed class TaskList(ITaskStore store)
{
    public Task<TaskWriteResult> CreateAsync(string ownerId, string title, CancellationToken cancellationToken)
        => store.AddAsync(ownerId, new TaskItem(title), cancellationToken);

    public Task<TaskWriteResult> RenameAsync(string ownerId, int id, string title, CancellationToken cancellationToken)
        => store.RenameAsync(ownerId, id, new TaskItem(title), cancellationToken);

    public Task<bool> SetCompletedAsync(string ownerId, int id, bool completed, CancellationToken cancellationToken)
        => store.SetCompletedAsync(ownerId, id, completed, cancellationToken);

    public Task<bool> DeleteAsync(string ownerId, int id, CancellationToken cancellationToken)
        => store.DeleteAsync(ownerId, id, cancellationToken);

    public Task<TaskSummary> FindAsync(string ownerId, int id, CancellationToken cancellationToken)
        => store.FindAsync(ownerId, id, cancellationToken);

    public Task<IReadOnlyList<TaskSummary>> ListAsync(string ownerId, bool? completed, CancellationToken cancellationToken)
        => store.ListAsync(ownerId, completed, cancellationToken);
}
