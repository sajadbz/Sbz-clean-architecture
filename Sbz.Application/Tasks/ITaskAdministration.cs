using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sbz.Application.Tasks;

public sealed record ManagedTask(int Id, string Title, bool IsCompleted, string OwnerId, string OwnerName);
public sealed record ManagedTaskPage(IReadOnlyList<ManagedTask> Tasks, int Total);
public sealed record TaskOwnerOption(string Id, string UserName);
public sealed record TaskTotals(int Total, int Completed, int Unassigned)
{
    public int Open => Total - Completed;
}
public sealed record OwnerTaskTotal(string UserName, int Total, int Completed)
{
    public int Open => Total - Completed;
}
public sealed record TaskAdministrationReport(TaskTotals Totals, IReadOnlyList<OwnerTaskTotal> Owners);

public enum ManagedTaskResult { Saved, NotFound, DuplicateTitle, InvalidOwner }

public interface ITaskAdministration
{
    Task<ManagedTaskPage> ListAsync(string search, bool unassigned, int page, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaskOwnerOption>> ListOwnersAsync(CancellationToken cancellationToken);
    Task<TaskAdministrationReport> ReportAsync(CancellationToken cancellationToken);
    Task<ManagedTask> FindAsync(int id, CancellationToken cancellationToken);
    Task<ManagedTaskResult> CreateAsync(string title, bool completed, string ownerId, CancellationToken cancellationToken);
    Task<ManagedTaskResult> UpdateAsync(int id, string title, bool completed, string ownerId, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
