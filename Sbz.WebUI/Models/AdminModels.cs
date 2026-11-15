using System.ComponentModel.DataAnnotations;
using Sbz.Application.Accounts;
using Sbz.Application.Tasks;
using Sbz.Domain.Tasks;

namespace Sbz.WebUI.Models;

public sealed record AdminDashboard(int Users, int Tasks, int UnassignedTasks);
public sealed record AdminAccountsList(AccountPage Results, string Search, int Page);
public sealed record AdminTasksList(ManagedTaskPage Results, string Search, bool Unassigned, int Page);

public sealed class AdminTaskForm
{
    public int Id { get; internal set; }

    [Required, StringLength(TaskItem.MaxTitleLength)]
    public string Title { get; set; }

    [Display(Name = "Completed")]
    public bool IsCompleted { get; set; }

    [Display(Name = "Owner username")]
    [StringLength(256)]
    public string OwnerName { get; set; }
}
