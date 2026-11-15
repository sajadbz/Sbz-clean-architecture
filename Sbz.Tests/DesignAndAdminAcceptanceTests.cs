using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Sbz.Tests.TaskListTests;

namespace Sbz.Tests;

public sealed class DesignAndAdminAcceptanceTests : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"sbz-design-{Guid.NewGuid():N}.db");

    [Fact(DisplayName = "FD-01 | Design | Public navigation stays focused on tasks")]
    [Trait("Category", "Design")]
    public async Task PublicSiteDoesNotExposeFunctionalDesign()
    {
        using var app = CreateApp();
        using var guest = CreateClient(app);
        var home = await guest.GetStringAsync("/");
        Assert.Contains("data-shell=\"public\"", home);
        Assert.Contains("href=\"/tasks\"", home);
        Assert.DoesNotContain("Functional Design", home);
        Assert.DoesNotContain("href=\"/design\"", home);
        using var formerPage = await guest.GetAsync("/design");
        Assert.Equal(HttpStatusCode.NotFound, formerPage.StatusCode);
    }

    [Fact(DisplayName = "FD-02 | Navigation | Admin uses its own layout")]
    [Trait("Category", "Navigation")]
    public async Task AdminUsesItsOwnLayout()
    {
        using var app = CreateApp();
        using var admin = CreateClient(app);
        await LoginAsync(admin, "administrator");
        var page = await admin.GetStringAsync("/admin");
        Assert.Contains("data-shell=\"admin\"", page);
        Assert.Contains("href=\"/admin/reports\"", page);
        Assert.Contains("Back to site", page);
        Assert.DoesNotContain("data-shell=\"public\"", page);
    }

    [Fact(DisplayName = "FD-03 | Ownership | Owner options include users beyond the first page")]
    [Trait("Category", "Ownership")]
    public async Task OwnerPickerIncludesEveryAccountAndValidatesIdentifiers()
    {
        using var app = CreateApp();
        using var admin = CreateClient(app);
        await LoginAsync(admin, "administrator");
        using var target = await CreateMemberClientAsync(app, "z-owner");
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            for (var i = 0; i < 26; i++)
                Assert.True((await users.CreateAsync(new IdentityUser($"account-{i:D2}"))).Succeeded);
        }

        var page = await admin.GetStringAsync("/admin/tasks/create");
        Assert.Contains("data-owner-search", page);
        Assert.Contains("<select", page);
        Assert.Contains(">Unassigned</option>", page);
        Assert.Contains(">account-00</option>", page);
        var owner = Regex.Match(page, "value=\"([^\"]+)\">z-owner</option>");
        Assert.True(owner.Success);
        Assert.True(Guid.TryParse(owner.Groups[1].Value, out _));

        const string route = "/admin/tasks/create";
        using var bad = await SubmitAsync(admin, route, route, new()
        {
            ["Title"] = "Invalid assignment", ["OwnerId"] = Guid.NewGuid().ToString()
        });
        Assert.Equal(HttpStatusCode.OK, bad.StatusCode);
        Assert.Contains("Select an existing owner.", await bad.Content.ReadAsStringAsync());

        using var good = await SubmitAsync(admin, route, route, new()
        {
            ["Title"] = "Assigned task", ["OwnerId"] = owner.Groups[1].Value
        });
        Assert.Equal(HttpStatusCode.Redirect, good.StatusCode);
        Assert.Contains("Assigned task", await target.GetStringAsync("/tasks"));
    }

    [Fact(DisplayName = "FD-04 | Filtering | Clear filters links to the unfiltered lists")]
    [Trait("Category", "Filtering")]
    public async Task ClearFiltersGoToUnfilteredLists()
    {
        using var app = CreateApp();
        using var admin = CreateClient(app);
        await LoginAsync(admin, "administrator");
        foreach (var (page, clean) in new[]
        {
            ("/admin/users?search=abc&page=2", "/admin/users"),
            ("/admin/tasks?search=abc&unassigned=true&page=2", "/admin/tasks")
        })
        {
            var html = await admin.GetStringAsync(page);
            Assert.Matches($"href=\"{clean}\"[^>]*>Clear filters</a>", html);
            Assert.Contains($"href=\"{clean}\"", html);
        }
    }

    [Fact(DisplayName = "FD-05 | Reporting | Reports use actual task data and require admin access")]
    [Trait("Category", "Reporting")]
    public async Task ReportsReflectPersistedDataAndRequireAdmin()
    {
        using var app = CreateApp();
        using var member = await CreateMemberClientAsync(app, "report-owner");
        var task = await CreateTaskAsync(member, "Completed task");
        using var complete = await PostAsync(member, "/tasks", $"/tasks/{task}/status", "completed", "true");
        Assert.Equal(HttpStatusCode.Redirect, complete.StatusCode);
        await CreateTaskAsync(member, "Open task");

        using var admin = CreateClient(app);
        await LoginAsync(admin, "administrator");
        using var created = await PostAsync(admin, "/admin/tasks/create", "/admin/tasks/create", "Title", "Unassigned task");
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var report = await admin.GetStringAsync("/admin/reports");
        Assert.Contains("data-metric=\"open\">2<", report);
        Assert.Contains("data-metric=\"completed\">1<", report);
        Assert.Contains("data-metric=\"unassigned\">1<", report);
        Assert.Contains("report-owner", report);
        using var denied = await member.GetAsync("/admin/reports");
        Assert.Equal("/account/denied", denied.Headers.Location?.AbsolutePath);
    }

    private TaskWebApplicationFactory CreateApp() => new(new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Pooling = false
    }.ToString(), withAdmin: true);

    public void Dispose() => File.Delete(databasePath);
}
