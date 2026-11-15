using System.ComponentModel.DataAnnotations;
using Sbz.Domain.Tasks;

namespace Sbz.WebUI.Models;

public class TaskForm
{
    public int Id { get; internal set; }

    [Required]
    [StringLength(TaskItem.MaxTitleLength)]
    public string Title { get; set; }
}
