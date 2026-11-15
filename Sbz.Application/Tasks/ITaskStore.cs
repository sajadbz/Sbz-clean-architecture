using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sbz.Domain.Tasks;

namespace Sbz.Application.Tasks;

public interface ITaskStore
{
    Task<TaskWriteResult> AddAsync(TaskItem item, CancellationToken cancellationToken);
    Task<TaskWriteResult> RenameAsync(int id, TaskItem title, CancellationToken cancellationToken);
    Task<bool> SetCompletedAsync(int id, bool completed, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<TaskSummary> FindAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaskSummary>> ListAsync(bool? completed, CancellationToken cancellationToken);
}
