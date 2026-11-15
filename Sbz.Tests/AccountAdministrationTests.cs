using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Xunit;
using static Sbz.Tests.TaskListTests;

namespace Sbz.Tests;

public sealed class AccountAdministrationTests : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"sbz-accounts-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task GuestsAreChallengedAndMembersCannotAccessAdministration()
    {
        using var app = CreateApp();
        using var guest = CreateClient(app);
        foreach (var path in new[] { "/tasks", "/admin" })
        {
            using var result = await guest.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
            Assert.Equal("/account/login", result.Headers.Location?.AbsolutePath);
        }

        using var member = await CreateMemberClientAsync(app);
        using var denied = await member.GetAsync("/admin/users");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.Equal("/account/denied", denied.Headers.Location?.AbsolutePath);
        using var deniedWrite = await PostAsync(member, "/tasks", "/admin/tasks/1/delete");
        Assert.Equal("/account/denied", deniedWrite.Headers.Location?.AbsolutePath);

        using var signedOut = await PostAsync(member, "/tasks", "/account/logout");
        Assert.Equal(HttpStatusCode.Redirect, signedOut.StatusCode);
        using var afterLogout = await member.GetAsync("/tasks");
        Assert.Equal("/account/login", afterLogout.Headers.Location?.AbsolutePath);

        using var login = await SubmitAsync(member, "/account/login", "/account/login", new()
        {
            ["UserName"] = "member",
            ["Password"] = TestPassword,
            ["ReturnUrl"] = "https://example.invalid/"
        });
        Assert.Equal("/tasks", login.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task EachOwnerCanUseTheSameTitleAndCanOnlyAccessTheirOwnTasks()
    {
        using var app = CreateApp();
        using var alice = await CreateMemberClientAsync(app, "alice");
        using var bob = await CreateMemberClientAsync(app, "bob");
        var privateId = await CreateTaskAsync(alice, "Alice private task");
        await CreateTaskAsync(alice, "Shared title");
        await CreateTaskAsync(bob, " SHARED TITLE ");
        Assert.DoesNotContain("Alice private task", await bob.GetStringAsync("/tasks"));

        foreach (var route in new[] { "edit", "delete" })
        {
            using var result = await bob.GetAsync($"/tasks/{privateId}/{route}");
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        using var rename = await PostAsync(bob, "/tasks", $"/tasks/{privateId}/edit", "Title", "Changed");
        using var status = await PostAsync(bob, "/tasks", $"/tasks/{privateId}/status", "completed", "true");
        using var delete = await PostAsync(bob, "/tasks", $"/tasks/{privateId}/delete");
        Assert.Equal(HttpStatusCode.NotFound, rename.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, status.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Contains("Alice private task", await alice.GetStringAsync("/tasks?completed=false"));
    }

    [Fact]
    public async Task AdminCanManageRolesDisableAccountsAndRevokeSessions()
    {
        using var app = CreateApp();
        using var member = await CreateMemberClientAsync(app);
        using var admin = CreateClient(app);
        await LoginAsync(admin, "administrator");

        var memberId = await FindAccountIdAsync(admin, "member");
        var adminId = await FindAccountIdAsync(admin, "administrator");
        var manage = $"/admin/users/{memberId}";

        using var selfDisable = await PostAsync(admin, "/admin/users", $"/admin/users/{adminId}", "action", "Disable");
        Assert.Equal("/account/denied", selfDisable.Headers.Location?.AbsolutePath);

        using var promote = await PostAsync(admin, manage, manage, "action", "Promote");
        Assert.Equal(HttpStatusCode.Redirect, promote.StatusCode);
        using var staleSession = await member.GetAsync("/admin");
        Assert.Equal("/account/login", staleSession.Headers.Location?.AbsolutePath);
        await LoginAsync(member, "member");
        Assert.Contains("Administration", await member.GetStringAsync("/admin"));

        using var demote = await PostAsync(admin, manage, manage, "action", "Demote");
        Assert.Equal(HttpStatusCode.Redirect, demote.StatusCode);
        using var staleAdmin = await member.GetAsync("/admin");
        Assert.Equal("/account/login", staleAdmin.Headers.Location?.AbsolutePath);

        await LoginAsync(member, "member");
        using var disable = await PostAsync(admin, manage, manage, "action", "Disable");
        Assert.Equal(HttpStatusCode.Redirect, disable.StatusCode);
        using var disabledSession = await member.GetAsync("/tasks");
        Assert.Equal("/account/login", disabledSession.Headers.Location?.AbsolutePath);
        using var deniedLogin = await SubmitAsync(member, "/account/login", "/account/login", new()
        {
            ["UserName"] = "member",
            ["Password"] = TestPassword
        });
        Assert.Equal(HttpStatusCode.OK, deniedLogin.StatusCode);

        using var enable = await PostAsync(admin, manage, manage, "action", "Enable");
        Assert.Equal(HttpStatusCode.Redirect, enable.StatusCode);
        await LoginAsync(member, "member");
        Assert.Contains("New task", await member.GetStringAsync("/tasks"));
    }

    [Fact]
    public async Task AdminCanTransferEditAndDeleteTasksAndAccountRemovalKeepsTasks()
    {
        using var app = CreateApp();
        using var first = await CreateMemberClientAsync(app, "first");
        using var second = await CreateMemberClientAsync(app, "second");
        var id = await CreateTaskAsync(first, "Transfer this task");
        await CreateTaskAsync(second, "Second task");
        var edit = $"/admin/tasks/{id}";

        using var admin = CreateClient(app);
        await LoginAsync(admin, "administrator");
        using var ownerMissing = await SubmitAsync(admin, edit, edit, new()
        {
            ["Title"] = "Transfer this task",
            ["OwnerName"] = "missing"
        });
        Assert.Contains("No account has that username.", await ownerMissing.Content.ReadAsStringAsync());
        using var duplicateTitle = await SubmitAsync(admin, edit, edit, new()
        {
            ["Title"] = "Second task",
            ["OwnerName"] = "second"
        });
        Assert.Contains("This owner already has a task with that title.", await duplicateTitle.Content.ReadAsStringAsync());

        using var transferred = await SubmitAsync(admin, edit, edit, new()
        {
            ["Title"] = "Transferred task",
            ["OwnerName"] = "second",
            ["IsCompleted"] = "true"
        });
        Assert.Equal(HttpStatusCode.Redirect, transferred.StatusCode);
        Assert.DoesNotContain("Transferred task", await first.GetStringAsync("/tasks"));
        Assert.Contains("Transferred task", await second.GetStringAsync("/tasks?completed=true"));

        var secondId = await FindAccountIdAsync(admin, "second");
        var account = $"/admin/users/{secondId}";
        Assert.Contains("Their tasks will remain", await admin.GetStringAsync(account + "/delete"));
        using var removed = await PostAsync(admin, account + "/delete", account, "action", "Delete");
        Assert.Equal(HttpStatusCode.Redirect, removed.StatusCode);
        Assert.Contains("Transferred task", await admin.GetStringAsync("/admin/tasks?unassigned=true"));

        var delete = edit + "/delete";
        using var deleted = await PostAsync(admin, delete, delete);
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        Assert.DoesNotContain("Transferred task", await admin.GetStringAsync("/admin/tasks"));
    }

    [Fact]
    public async Task AdminCanCreateAccountsAndAssignNewTasks()
    {
        using var app = CreateApp();
        using var admin = CreateClient(app);
        await LoginAsync(admin, "administrator");
        const string account = "/admin/users/create";
        var registration = new Dictionary<string, string>
        {
            ["UserName"] = "managed-user",
            ["Password"] = TestPassword,
            ["ConfirmPassword"] = TestPassword
        };
        using var created = await SubmitAsync(admin, account, account, registration);
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        Assert.Contains("managed-user", await admin.GetStringAsync("/admin/users?search=managed-user"));

        using var duplicateAccount = await SubmitAsync(admin, account, account, registration);
        Assert.Equal(HttpStatusCode.OK, duplicateAccount.StatusCode);
        Assert.Contains("already taken", await duplicateAccount.Content.ReadAsStringAsync());

        const string task = "/admin/tasks/create";
        var fields = new Dictionary<string, string>
        {
            ["Title"] = "Task from administrator",
            ["OwnerName"] = "managed-user",
            ["IsCompleted"] = "true"
        };
        using var added = await SubmitAsync(admin, task, task, fields);
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
        using var duplicateTask = await SubmitAsync(admin, task, task, fields);
        Assert.Contains("This owner already has a task with that title.", await duplicateTask.Content.ReadAsStringAsync());

        using var member = CreateClient(app);
        await LoginAsync(member, "managed-user");
        Assert.Contains("Task from administrator", await member.GetStringAsync("/tasks?completed=true"));
        using var denied = await member.GetAsync("/admin");
        Assert.Equal("/account/denied", denied.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task AdminCanUnlockAnAccountAfterFailedLoginAttempts()
    {
        using var app = CreateApp();
        using var registered = await CreateMemberClientAsync(app);
        using var member = CreateClient(app);
        const string login = "/account/login";

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var rejected = await SubmitAsync(member, login, login, new()
            {
                ["UserName"] = "member",
                ["Password"] = "Wrong password"
            });
            Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        }

        using var locked = await SubmitAsync(member, login, login, new()
        {
            ["UserName"] = "member",
            ["Password"] = TestPassword
        });
        Assert.Equal(HttpStatusCode.OK, locked.StatusCode);

        using var admin = CreateClient(app);
        await LoginAsync(admin, "administrator");
        var memberId = await FindAccountIdAsync(admin, "member");
        var account = $"/admin/users/{memberId}";
        using var unlocked = await PostAsync(admin, account, account, "action", "Unlock");
        Assert.Equal(HttpStatusCode.Redirect, unlocked.StatusCode);
        await LoginAsync(member, "member");
    }

    private TaskWebApplicationFactory CreateApp() => new(new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Pooling = false
    }.ToString(), withAdmin: true);

    private static async Task<string> FindAccountIdAsync(HttpClient admin, string username)
    {
        var page = await admin.GetStringAsync($"/admin/users?search={username}");
        return Assert.Single(Regex.Matches(page, "href=\"/admin/users/([^\"/]+)\""), match => Guid.TryParse(match.Groups[1].Value, out _)).Groups[1].Value;
    }

    public void Dispose() => File.Delete(databasePath);
}
