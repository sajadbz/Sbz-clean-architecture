using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Sbz.Application.Tasks;
using Sbz.WebUI.Models;

namespace Sbz.WebUI.Controllers;

[Route("tasks")]
[AutoValidateAntiforgeryToken]
public sealed class TasksController(TaskList taskList) : Controller
{
    private const string DuplicateTitleMessage = "A task with this title already exists.";

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] bool? completed, CancellationToken cancellationToken)
        => View(await LoadListAsync(new TaskListViewModel(), completed, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Index(TaskListViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            if (await taskList.CreateAsync(model.Title, cancellationToken) == TaskWriteResult.Saved)
                return RedirectToAction(nameof(Index));

            ModelState.AddModelError(nameof(model.Title), DuplicateTitleMessage);
        }

        return View(await LoadListAsync(model, null, cancellationToken));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var task = await taskList.FindAsync(id, cancellationToken);
        if (task is null)
            return NotFound();

        return View(new TaskForm { Id = task.Id, Title = task.Title });
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, TaskForm model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await taskList.RenameAsync(id, model.Title, cancellationToken);
            if (result == TaskWriteResult.Saved)
                return RedirectToAction(nameof(Index));
            if (result == TaskWriteResult.NotFound)
                return NotFound();

            ModelState.AddModelError(nameof(model.Title), DuplicateTitleMessage);
        }
        else if (await taskList.FindAsync(id, cancellationToken) is null)
        {
            return NotFound();
        }

        model.Id = id;
        return View(model);
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromForm] bool? completed, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || !completed.HasValue)
            return BadRequest();

        return await taskList.SetCompletedAsync(id, completed.Value, cancellationToken)
            ? RedirectToAction(nameof(Index))
            : NotFound();
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var task = await taskList.FindAsync(id, cancellationToken);
        return task is null ? NotFound() : View(task);
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
        => await taskList.DeleteAsync(id, cancellationToken)
            ? RedirectToAction(nameof(Index))
            : NotFound();

    private async Task<TaskListViewModel> LoadListAsync(
        TaskListViewModel model, bool? completed, CancellationToken cancellationToken)
    {
        model.Completed = completed;
        model.Tasks = await taskList.ListAsync(completed, cancellationToken);
        return model;
    }
}
