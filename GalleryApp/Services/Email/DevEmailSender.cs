using Microsoft.AspNetCore.Identity.UI.Services;

namespace GalleryApp.Services.Email;

public sealed class DevEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return Task.CompletedTask;
    }
}
