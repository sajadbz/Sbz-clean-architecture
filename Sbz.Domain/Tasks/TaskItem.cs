using System;

namespace Sbz.Domain.Tasks;

public sealed class TaskItem
{
    public const int MaxTitleLength = 100;

    private TaskItem() { }

    public TaskItem(string title)
    {
        Title = title?.Trim() ?? string.Empty;
        if (Title.Length is 0 or > MaxTitleLength)
            throw new ArgumentException($"Enter a title between 1 and {MaxTitleLength} characters.", nameof(title));

        NormalizedTitle = Title.ToUpperInvariant();
    }

    public int Id { get; private set; }
    public string Title { get; private set; }
    public string NormalizedTitle { get; private set; }
    public bool IsCompleted { get; private set; }
}
