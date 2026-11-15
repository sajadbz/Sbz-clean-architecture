# Sbz Clean Architecture

An ASP.NET Core MVC sample with username/password accounts, private task lists, and a Bootstrap administration panel. Users can create, rename, complete, reopen, filter, and delete their own tasks. Titles are unique per owner, ignoring case and surrounding whitespace.

## Run locally

Use the .NET 10 SDK and the HTTPS launch profile. Sign-in cookies require HTTPS.

```powershell
dotnet run --project Sbz.WebUI
dotnet test Sbz.sln
```

Open **Register** for a regular account, then **Tasks**. SQLite uses `sbz-tasks.db` in the app's working directory by default. Set `ConnectionStrings__Tasks` to use a different database. The app applies EF Core migrations on startup. Existing tasks are preserved and appear under **Admin → Manage tasks → Unassigned only** until assigned to a username. Back up an existing database before updating. Reverting to the shared-list schema requires that backup.

Set `AutoMapper__LicenseKey` in the app environment when using the existing AutoMapper configuration.

## First administrator

On Windows, start the app with the interactive setup script:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Start-WithAdmin.ps1
```

Choose an unused username and a fresh password when prompted. For manual setup, set the bootstrap values before starting the app:

```powershell
$env:AdminBootstrap__UserName = Read-Host 'Administrator username'
$adminPassword = Read-Host 'Administrator password' -AsSecureString
$env:AdminBootstrap__Password = [System.Net.NetworkCredential]::new('', $adminPassword).Password
Remove-Variable adminPassword
dotnet run --project Sbz.WebUI
```

After the account is provisioned, stop the app and remove the bootstrap values from your environment:

```powershell
Remove-Item Env:AdminBootstrap__UserName, Env:AdminBootstrap__Password
```

Start the app again and sign in with that account. Bootstrap creates a new administrator account and refuses to promote an existing regular user. Keep the password out of source control.

## Administration

- The dashboard shows account, task, and unassigned-task counts.
- Administrators can search and create user accounts, grant or remove the administrator role, disable or enable accounts, unlock accounts, and delete other accounts. Account deletion preserves the user's tasks as unassigned.
- Administrators can search, create, edit, complete, delete, assign, and reassign tasks. Leaving the owner blank makes the task visible in the admin panel only.
- Role and account-status changes revoke the affected account's existing sessions. Administrators cannot use the panel to modify or delete their own account.

## Project responsibilities

| Project | Responsibility |
| --- | --- |
| `Sbz.Domain` | Validates and normalizes task titles. |
| `Sbz.Application` | Defines task operations and administration contracts. |
| `Sbz.Infrastructure` | Implements the contracts with EF Core and SQLite and persists ASP.NET Core Identity accounts and roles. |
| `Sbz.WebUI` | Handles authentication, authorization, Bootstrap views, MVC validation, and antiforgery. |

The tests cover task workflows, cross-account access, administrative actions, session revocation, and upgrading an existing database. GitHub Actions builds and tests the solution on pull requests.

## Functional design and tests

Read the [Functional Design](docs/functional-design.md) and its [BDD scenarios](docs/acceptance/design-and-admin.feature) in the repository. Each [GitHub Actions run](https://github.com/sajadbz/Sbz-clean-architecture/actions) shows a result per requirement. Expand **Optional test details** for category, duration, and timestamps, or download the test-results artifact for raw TRX. The [project wiki](https://github.com/sajadbz/Sbz-clean-architecture/wiki) covers setup, architecture, and administration.

The public layout and administration workspace use Bootstrap. The administration layout draws on the information structure of the [Bootstrap dashboard example](https://getbootstrap.com/docs/5.3/examples/dashboard/) and [Tabler](https://docs.tabler.io/ui/getting-started).
