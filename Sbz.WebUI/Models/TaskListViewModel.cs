using System;
using System.Collections.Generic;
using Sbz.Application.Tasks;

namespace Sbz.WebUI.Models;

public sealed class TaskListViewModel : TaskForm
{
    public IReadOnlyList<TaskSummary> Tasks { get; internal set; } = Array.Empty<TaskSummary>();
    public bool? Completed { get; internal set; }
}
