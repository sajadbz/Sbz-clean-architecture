using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sbz.Domain.Tasks;

namespace Sbz.Application.Tasks;

public interface ITaskStore
{
    Task<TaskWriteResult> AddAsync(string ownerId, TaskItem item, CancellationToken cancellationToken);
    Task<TaskWriteResult> RenameAsync(string ownerId, int id, TaskItem title, CancellationToken cancellationToken);
    Task<bool> SetCompletedAsync(string ownerId, int id, bool completed, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string ownerId, int id, CancellationToken cancellationToken);
    Task<TaskSummary> FindAsync(string ownerId, int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaskSummary>> ListAsync(string ownerId, bool? completed, CancellationToken cancellationToken);
}
