using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sbz.Application.Tasks;

public sealed record ManagedTask(int Id, string Title, bool IsCompleted, string OwnerId, string OwnerName);
public sealed record ManagedTaskPage(IReadOnlyList<ManagedTask> Tasks, int Total);
public enum ManagedTaskResult { Saved, NotFound, DuplicateTitle, InvalidOwner }

public interface ITaskAdministration
{
    Task<ManagedTaskPage> ListAsync(string search, bool unassigned, int page, CancellationToken cancellationToken);
    Task<ManagedTask> FindAsync(int id, CancellationToken cancellationToken);
    Task<ManagedTaskResult> CreateAsync(string title, bool completed, string ownerName, CancellationToken cancellationToken);
    Task<ManagedTaskResult> UpdateAsync(int id, string title, bool completed, string ownerName, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
