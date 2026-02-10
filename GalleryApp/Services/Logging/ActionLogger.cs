using System.Security.Claims;
using GalleryApp.Data;
using GalleryApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GalleryApp.Services.Logging;

public class ActionLogger : IActionLogger
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public ActionLogger(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string action, string? entityType = null, string? entityId = null, string? details = null, CancellationToken ct = default)
    {
        var ctx = _http.HttpContext;

        string? userId = null;
        string? email = null;

        if (ctx?.User?.Identity?.IsAuthenticated == true)
        {
            userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            email = ctx.User.FindFirstValue(ClaimTypes.Email) ?? ctx.User.Identity?.Name;

            if (!string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(email))
            {
                email = await _db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync(ct);
            }
        }

        var log = new ActionLog
        {
            UserId = userId,
            UserEmail = email,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            TimestampUtc = DateTime.UtcNow,
            Details = details
        };

        _db.ActionLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
