using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sbz.Domain.Tasks;

namespace Sbz.Application.Tasks;

public sealed class TaskList(ITaskStore store)
{
    public Task<TaskWriteResult> CreateAsync(string title, CancellationToken cancellationToken)
        => store.AddAsync(new TaskItem(title), cancellationToken);

    public Task<TaskWriteResult> RenameAsync(int id, string title, CancellationToken cancellationToken)
        => store.RenameAsync(id, new TaskItem(title), cancellationToken);

    public Task<bool> SetCompletedAsync(int id, bool completed, CancellationToken cancellationToken)
        => store.SetCompletedAsync(id, completed, cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        => store.DeleteAsync(id, cancellationToken);

    public Task<TaskSummary> FindAsync(int id, CancellationToken cancellationToken)
        => store.FindAsync(id, cancellationToken);

    public Task<IReadOnlyList<TaskSummary>> ListAsync(bool? completed, CancellationToken cancellationToken)
        => store.ListAsync(completed, cancellationToken);
}
