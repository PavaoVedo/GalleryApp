namespace GalleryApp.Services.Logging.Commands;

public class LogActionCommand : IActionCommand
{
    public string Action { get; }
    public string? EntityType { get; }
    public string? EntityId { get; }
    public string? Details { get; }

    public LogActionCommand(string action, string? entityType = null, string? entityId = null, string? details = null)
    {
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Details = details;
    }

    public Task ExecuteAsync(IActionLogger logger, CancellationToken ct = default)
        => logger.LogAsync(Action, EntityType, EntityId, Details, ct);
}
