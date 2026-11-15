using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sbz.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261115000000_CreateTaskList")]
public sealed class CreateTaskList : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The create/list preview used EnsureCreated and already has this table and index.
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "Tasks" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Tasks" PRIMARY KEY AUTOINCREMENT,
                "Title" TEXT NOT NULL,
                "NormalizedTitle" TEXT NOT NULL
            );
            """);
        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Tasks_NormalizedTitle" ON "Tasks" ("NormalizedTitle");
            """);
        migrationBuilder.AddColumn<bool>("IsCompleted", "Tasks", type: "INTEGER", nullable: false, defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable("Tasks");
}
