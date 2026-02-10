using GalleryApp.Data;
using GalleryApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace GalleryApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var latest = await _db.Photos
                .Include(p => p.User)
                .Include(p => p.PhotoHashtags).ThenInclude(ph => ph.Hashtag)
                .OrderByDescending(p => p.UploadedAtUtc)
                .Take(10)
                .ToListAsync(ct);

            return View(latest);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
