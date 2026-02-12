namespace GalleryApp.Services.Logging.Commands;

public class ActionCommandDispatcher
{
    private readonly IActionLogger _logger;

    public ActionCommandDispatcher(IActionLogger logger)
    {
        _logger = logger;
    }

    public Task DispatchAsync(IActionCommand command, CancellationToken ct = default)
        => command.ExecuteAsync(_logger, ct);
}
