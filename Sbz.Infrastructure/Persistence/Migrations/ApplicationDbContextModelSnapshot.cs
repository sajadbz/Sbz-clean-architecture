using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Sbz.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
internal sealed class ApplicationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.15");
        modelBuilder.Entity("Sbz.Domain.Tasks.TaskItem", task =>
        {
            task.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            task.Property<bool>("IsCompleted").HasColumnType("INTEGER");
            task.Property<string>("NormalizedTitle").IsRequired().HasColumnType("TEXT");
            task.Property<string>("Title").IsRequired().HasMaxLength(100).HasColumnType("TEXT");
            task.HasKey("Id");
            task.HasIndex("NormalizedTitle").IsUnique();
            task.ToTable("Tasks");
        });
    }
}
