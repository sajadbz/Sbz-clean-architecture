using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sbz.Application.Accounts;
using Sbz.Application.Tasks;
using Sbz.WebUI.Models;

namespace Sbz.WebUI.Controllers;

[Authorize(Roles = AccountRoles.Administrator)]
[AutoValidateAntiforgeryToken]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route("admin")]
public sealed class AdminController(IAccountAdministration accounts, ITaskAdministration tasks) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await accounts.ListAsync(null, 1, cancellationToken);
        var allTasks = await tasks.ListAsync(null, false, 1, cancellationToken);
        var unassigned = await tasks.ListAsync(null, true, 1, cancellationToken);
        return View(new AdminDashboard(users.Total, allTasks.Total, unassigned.Total));
    }

    [HttpGet("users")]
    public async Task<IActionResult> Accounts(string search, int page, CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        var result = await accounts.ListAsync(search, page, cancellationToken);
        return View(new AdminAccountsList(result, search, page));
    }

    [HttpGet("users/create")]
    public IActionResult CreateAccount() => View(new RegistrationForm());

    [HttpPost("users/create")]
    public async Task<IActionResult> CreateAccount(RegistrationForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(form);

        var result = await accounts.CreateAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
            form.UserName, form.Password, cancellationToken);
        if (result.Result == AccountResult.Saved)
            return RedirectToAction(nameof(Accounts), new { search = form.UserName });
        if (result.Result == AccountResult.Forbidden)
            return Forbid();

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error);
        return View(form);
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> Account([FromRoute] string id, CancellationToken cancellationToken)
    {
        var account = await accounts.FindAsync(id, cancellationToken);
        return account is null ? NotFound() : View(account);
    }

    [HttpPost("users/{id}")]
    public async Task<IActionResult> ChangeAccount(
        [FromRoute] string id, [FromForm] AccountAction? action, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || !action.HasValue || !Enum.IsDefined(action.Value))
            return BadRequest();
        var result = await accounts.ChangeAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), id, action.Value, cancellationToken);
        return result switch
        {
            AccountResult.Saved when action == AccountAction.Delete => RedirectToAction(nameof(Accounts)),
            AccountResult.Saved => RedirectToAction(nameof(Account), new { id }),
            AccountResult.NotFound => NotFound(),
            AccountResult.Forbidden => Forbid(),
            _ => Conflict("The account could not be updated.")
        };
    }

    [HttpGet("users/{id}/delete")]
    public async Task<IActionResult> DeleteAccount([FromRoute] string id, CancellationToken cancellationToken)
    {
        if (id == User.FindFirstValue(ClaimTypes.NameIdentifier))
            return Forbid();
        var account = await accounts.FindAsync(id, cancellationToken);
        return account is null ? NotFound() : View(account);
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> Tasks(string search, bool unassigned, int page, CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        var result = await tasks.ListAsync(search, unassigned, page, cancellationToken);
        return View(new AdminTasksList(result, search, unassigned, page));
    }

    [HttpGet("tasks/create")]
    public IActionResult CreateTask() => View(nameof(EditTask), new AdminTaskForm());

    [HttpPost("tasks/create")]
    public async Task<IActionResult> CreateTask(AdminTaskForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(nameof(EditTask), form);

        var result = await tasks.CreateAsync(form.Title, form.IsCompleted, form.OwnerName, cancellationToken);
        if (result == ManagedTaskResult.Saved)
            return RedirectToAction(nameof(Tasks));

        AddTaskErrors(form, result);
        return View(nameof(EditTask), form);
    }

    [HttpGet("tasks/{id:int}")]
    public async Task<IActionResult> EditTask([FromRoute] int id, CancellationToken cancellationToken)
    {
        var task = await tasks.FindAsync(id, cancellationToken);
        return task is null ? NotFound() : View(new AdminTaskForm
        {
            Id = id,
            Title = task.Title,
            OwnerName = task.OwnerName,
            IsCompleted = task.IsCompleted
        });
    }

    [HttpPost("tasks/{id:int}")]
    public async Task<IActionResult> EditTask([FromRoute] int id, AdminTaskForm form, CancellationToken cancellationToken)
    {
        form.Id = id;
        if (!ModelState.IsValid)
            return View(form);
        var result = await tasks.UpdateAsync(id, form.Title, form.IsCompleted, form.OwnerName, cancellationToken);
        if (result == ManagedTaskResult.Saved)
            return RedirectToAction(nameof(Tasks));
        if (result == ManagedTaskResult.NotFound)
            return NotFound();

        AddTaskErrors(form, result);
        return View(form);
    }

    [HttpGet("tasks/{id:int}/delete")]
    public async Task<IActionResult> DeleteTask([FromRoute] int id, CancellationToken cancellationToken)
    {
        var task = await tasks.FindAsync(id, cancellationToken);
        return task is null ? NotFound() : View(task);
    }

    [HttpPost("tasks/{id:int}/delete")]
    public async Task<IActionResult> DeleteTaskConfirmed([FromRoute] int id, CancellationToken cancellationToken)
        => await tasks.DeleteAsync(id, cancellationToken) ? RedirectToAction(nameof(Tasks)) : NotFound();

    private void AddTaskErrors(AdminTaskForm form, ManagedTaskResult result)
    {
        if (result == ManagedTaskResult.DuplicateTitle)
            ModelState.AddModelError(nameof(form.Title), "This owner already has a task with that title.");
        else if (result == ManagedTaskResult.InvalidOwner)
            ModelState.AddModelError(nameof(form.OwnerName), "No account has that username.");
    }

    private static int NormalizePage(int page) => Math.Clamp(page, 1, int.MaxValue / 25);
}
