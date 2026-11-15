# Sbz Clean Architecture

An ASP.NET Core MVC sample for managing a shared task list. Create, rename, and delete tasks, mark them completed or reopen them, and filter the list by status. Titles are unique in the list, ignoring case and surrounding whitespace.

## Run locally

Use the .NET 10 SDK.

```shell
dotnet run --project Sbz.WebUI
dotnet test Sbz.sln
```

Open **Tasks** from the navigation. The app applies EF Core migrations on startup. SQLite uses `sbz-tasks.db` in the app's working directory by default. Set `ConnectionStrings__Tasks` to use a different database. The first migration upgrades task databases created by the earlier create/list preview and preserves existing tasks.

Set `AutoMapper__LicenseKey` in the app environment when using the existing AutoMapper configuration.

## Project responsibilities

| Project | Responsibility in the tasks feature |
| --- | --- |
| `Sbz.Domain` | Validates task titles and defines the normalized title. |
| `Sbz.Application` | Exposes task operations and defines the storage contract. |
| `Sbz.Infrastructure` | Implements the contract with EF Core and SQLite. A unique index protects creates and renames from concurrent duplicates. |
| `Sbz.WebUI` | Provides the Bootstrap UI, MVC form validation, antiforgery, and confirmation for deletes. |

The tests cover title rules and the MVC and SQLite paths for CRUD, status changes, filters, invalid requests, and upgrading an existing database.
