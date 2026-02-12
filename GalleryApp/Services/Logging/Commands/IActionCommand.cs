namespace GalleryApp.Services.Logging.Commands;

public interface IActionCommand
{
    Task ExecuteAsync(IActionLogger logger, CancellationToken ct = default);
}
