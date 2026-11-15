using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Sbz.Domain.Tasks;
using Sbz.WebUI;
using Xunit;

namespace Sbz.Tests;

public sealed class TaskListTests : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"sbz-tasks-{Guid.NewGuid():N}.db");

    private string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Pooling = false
    }.ToString();

    [Fact]
    public void TaskTitlesAreTrimmedAndComparedWithoutCase()
    {
        var first = new TaskItem("  Review architecture  ");
        var second = new TaskItem("review architecture");

        Assert.Equal("Review architecture", first.Title);
        Assert.Equal(first.NormalizedTitle, second.NormalizedTitle);
        Assert.False(first.IsCompleted);
    }

    [Fact]
    public void DomainRejectsBlankAndOverlyLongTitles()
    {
        Assert.Throws<ArgumentException>(() => new TaskItem(null));
        Assert.Throws<ArgumentException>(() => new TaskItem("  "));
        Assert.Throws<ArgumentException>(() => new TaskItem(new string('x', TaskItem.MaxTitleLength + 1)));
        Assert.Equal(TaskItem.MaxTitleLength, new TaskItem(new string('x', TaskItem.MaxTitleLength)).Title.Length);
    }

    [Fact]
    public async Task CreatePersistsAcrossRestartsAndRejectsNormalizedDuplicates()
    {
        using (var app = CreateApp())
        using (var client = await CreateMemberClientAsync(app))
        {
            Assert.Contains("No tasks to display.", await client.GetStringAsync("/tasks"));
            await CreateTaskAsync(client, "Review architecture");
        }

        using var reopened = CreateApp();
        using var reopenedClient = CreateClient(reopened);
        await LoginAsync(reopenedClient, "member");
        Assert.Contains("Review architecture", await reopenedClient.GetStringAsync("/tasks"));

        using var duplicate = await PostAsync(reopenedClient, "/tasks", "/tasks", "Title", " REVIEW ARCHITECTURE ");
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Contains("A task with this title already exists.", await duplicate.Content.ReadAsStringAsync());
        Assert.Single(Regex.Matches(await reopenedClient.GetStringAsync("/tasks"), "/tasks/[0-9]+/edit"));
    }

    [Fact]
    public async Task EditAllowsKeepingTheTitleAndRejectsAnotherTasksTitle()
    {
        using var app = CreateApp();
        using var client = await CreateMemberClientAsync(app);
        var taskId = await CreateTaskAsync(client, "First task");
        await CreateTaskAsync(client, "Second task");
        var edit = $"/tasks/{taskId}/edit";

        Assert.Contains("value=\"First task\"", await client.GetStringAsync(edit));
        using var sameTitle = await PostAsync(client, edit, edit, "Title", " FIRST TASK ");
        Assert.Equal(HttpStatusCode.Redirect, sameTitle.StatusCode);

        using var duplicate = await PostAsync(client, edit, edit, "Title", "SECOND TASK");
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Contains("A task with this title already exists.", await duplicate.Content.ReadAsStringAsync());
        Assert.Contains("value=\"FIRST TASK\"", await client.GetStringAsync(edit));

        using var renamed = await PostAsync(client, edit, edit, "Title", "Reviewed task");
        Assert.Equal(HttpStatusCode.Redirect, renamed.StatusCode);
        var page = await client.GetStringAsync("/tasks");
        Assert.Contains("Reviewed task", page);
        Assert.DoesNotContain("FIRST TASK", page);
    }

    [Fact]
    public async Task TasksCanBeCompletedFilteredAndReopened()
    {
        using var app = CreateApp();
        using var client = await CreateMemberClientAsync(app);
        var id = await CreateTaskAsync(client, "Track this task");
        var status = $"/tasks/{id}/status";

        using var completed = await PostAsync(client, "/tasks", status, "completed", "true");
        Assert.Equal(HttpStatusCode.Redirect, completed.StatusCode);
        Assert.Contains("Track this task", await client.GetStringAsync("/tasks?completed=true"));
        Assert.DoesNotContain("Track this task", await client.GetStringAsync("/tasks?completed=false"));

        using var reopened = await PostAsync(client, "/tasks", status, "completed", "false");
        Assert.Equal(HttpStatusCode.Redirect, reopened.StatusCode);
        Assert.Contains("Track this task", await client.GetStringAsync("/tasks?completed=false"));
        Assert.DoesNotContain("Track this task", await client.GetStringAsync("/tasks?completed=true"));
    }

    [Fact]
    public async Task DeleteRequiresConfirmationAndRemovesTheTask()
    {
        using var app = CreateApp();
        using var client = await CreateMemberClientAsync(app);
        var id = await CreateTaskAsync(client, "Delete this task");
        var delete = $"/tasks/{id}/delete";

        Assert.Contains("Delete this task", await client.GetStringAsync(delete));
        Assert.Contains("Delete this task", await client.GetStringAsync("/tasks"));

        using var deleted = await PostAsync(client, delete, delete);
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        Assert.DoesNotContain("Delete this task", await client.GetStringAsync("/tasks"));

        using var missing = await client.GetAsync(delete);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await CreateTaskAsync(client, "Delete this task");
    }

    [Theory]
    [InlineData("/tasks")]
    [InlineData("/tasks/1/edit")]
    [InlineData("/tasks/1/delete")]
    [InlineData("/tasks/1/status")]
    public async Task MutatingRoutesRequireAnAntiforgeryToken(string route)
    {
        using var app = CreateApp();
        using var client = await CreateMemberClientAsync(app);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>());
        using var response = await client.PostAsync(route, form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/tasks/999/edit", "Title", "New title")]
    [InlineData("/tasks/999/delete", null, null)]
    [InlineData("/tasks/999/status", "completed", "true")]
    public async Task WritesToMissingTasksReturnNotFound(string route, string field, string value)
    {
        using var app = CreateApp();
        using var client = await CreateMemberClientAsync(app);
        using var response = await PostAsync(client, "/tasks", route, field, value);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InvalidFormsDoNotMutateTheTask()
    {
        using var app = CreateApp();
        using var client = await CreateMemberClientAsync(app);
        var id = await CreateTaskAsync(client, "Keep this title");
        var edit = $"/tasks/{id}/edit";

        using var blank = await PostAsync(client, "/tasks", "/tasks", "Title", " ");
        Assert.Equal(HttpStatusCode.OK, blank.StatusCode);
        Assert.Contains("The Title field is required.", await blank.Content.ReadAsStringAsync());

        using var tooLong = await PostAsync(client, edit, edit, "Title", new string('x', TaskItem.MaxTitleLength + 1));
        Assert.Equal(HttpStatusCode.OK, tooLong.StatusCode);
        Assert.Contains("maximum length", await tooLong.Content.ReadAsStringAsync());

        using var invalidStatus = await PostAsync(client, "/tasks", $"/tasks/{id}/status", "completed", "invalid");
        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
        Assert.Contains("Keep this title", await client.GetStringAsync("/tasks?completed=false"));
    }

    [Fact]
    public async Task MigrationPreservesTasksCreatedByTheFirstVersion()
    {
        await using (var connection = new SqliteConnection(ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE Tasks (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    NormalizedTitle TEXT NOT NULL
                );
                CREATE UNIQUE INDEX IX_Tasks_NormalizedTitle ON Tasks (NormalizedTitle);
                INSERT INTO Tasks (Title, NormalizedTitle) VALUES ('Existing task', 'EXISTING TASK');
                """;
            await command.ExecuteNonQueryAsync();
        }

        using var app = CreateApp(withAdmin: true);
        using var client = await CreateMemberClientAsync(app);
        Assert.DoesNotContain("Existing task", await client.GetStringAsync("/tasks"));
        using var admin = CreateClient(app);
        await LoginAsync(admin, "administrator");
        Assert.Contains("Existing task", await admin.GetStringAsync("/admin/tasks?unassigned=true"));
        using var assigned = await SubmitAsync(admin, "/admin/tasks/1", "/admin/tasks/1", new()
        {
            ["Title"] = "Existing task",
            ["OwnerId"] = await AccountAdministrationTests.FindAccountIdAsync(admin, "member"),
            ["IsCompleted"] = "false"
        });
        Assert.Equal(HttpStatusCode.Redirect, assigned.StatusCode);
        Assert.Contains("Existing task", await client.GetStringAsync("/tasks?completed=false"));
        using var completed = await PostAsync(client, "/tasks", "/tasks/1/status", "completed", "true");
        Assert.Equal(HttpStatusCode.Redirect, completed.StatusCode);
        Assert.Contains("Existing task", await client.GetStringAsync("/tasks?completed=true"));
    }

    public void Dispose() => File.Delete(databasePath);

    private TaskWebApplicationFactory CreateApp(bool withAdmin = false) => new(ConnectionString, withAdmin);

    internal static HttpClient CreateClient(WebApplicationFactory<Program> app)
        => app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

    internal static async Task<int> CreateTaskAsync(HttpClient client, string title)
    {
        using var response = await PostAsync(client, "/tasks", "/tasks", "Title", title);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/tasks", response.Headers.Location?.OriginalString);

        var page = await client.GetStringAsync("/tasks");
        var editLinks = Regex.Matches(page, "/tasks/([0-9]+)/edit");
        Assert.NotEmpty(editLinks);
        return int.Parse(editLinks[^1].Groups[1].Value);
    }

    internal static Task<HttpResponseMessage> PostAsync(
        HttpClient client, string formPage, string route, string field = null, string value = null)
    {
        return SubmitAsync(client, formPage, route, field is null ? new() : new() { [field] = value });
    }

    internal static async Task<HttpResponseMessage> SubmitAsync(HttpClient client, string formPage, string route, Dictionary<string, string> fields)
    {
        var page = await client.GetStringAsync(formPage);
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "The form must include an antiforgery token.");

        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value);
        using var form = new FormUrlEncodedContent(fields);
        return await client.PostAsync(route, form);
    }

    internal const string TestPassword = "Local sample passphrase 91!";

    internal static async Task<HttpClient> CreateMemberClientAsync(WebApplicationFactory<Program> app, string name = "member")
    {
        var client = CreateClient(app);
        using var registered = await SubmitAsync(client, "/account/register", "/account/register", new()
        {
            ["UserName"] = name,
            ["Password"] = TestPassword,
            ["ConfirmPassword"] = TestPassword
        });
        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);
        return client;
    }

    internal static async Task LoginAsync(HttpClient client, string name)
    {
        using var response = await SubmitAsync(client, "/account/login", "/account/login", new()
        {
            ["UserName"] = name,
            ["Password"] = TestPassword
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    internal sealed class TaskWebApplicationFactory(string connectionString, bool withAdmin = false) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var settings = new Dictionary<string, string> { ["ConnectionStrings:Tasks"] = connectionString };
                if (withAdmin)
                {
                    settings["AdminBootstrap:UserName"] = "administrator";
                    settings["AdminBootstrap:Password"] = TestPassword;
                }
                configuration.AddInMemoryCollection(settings);
            });
        }
    }
}
