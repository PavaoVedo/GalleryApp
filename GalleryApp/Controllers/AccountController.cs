using GalleryApp.Data;
using GalleryApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GalleryApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public AccountController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var uploadsCount = await _db.Photos.CountAsync(p => p.UserId == user.Id, ct);

            var vm = new AccountViewModel
            {
                Email = user.Email ?? "",
                CurrentPlan = user.CurrentPlan,
                UploadsCount = uploadsCount,
                NewPlan = user.CurrentPlan
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePlan(AccountViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!Enum.IsDefined(typeof(Plan), model.NewPlan))
            {
                ModelState.AddModelError(nameof(model.NewPlan), "Invalid plan selected.");
                return await RebuildAndReturnIndex(model, ct);
            }

            user.CurrentPlan = model.NewPlan;

            _db.Users.Update(user);
            await _db.SaveChangesAsync(ct);

            TempData["StatusMessage"] = $"Plan updated to: {user.CurrentPlan}";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> RebuildAndReturnIndex(AccountViewModel model, CancellationToken ct)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                model.Email = user.Email ?? model.Email;
                model.CurrentPlan = user.CurrentPlan;
                model.UploadsCount = await _db.Photos.CountAsync(p => p.UserId == user.Id, ct);
            }

            return View("Index", model);
        }
    }

    public class AccountViewModel
    {
        public string Email { get; set; } = "";

        public Plan CurrentPlan { get; set; }

        public int UploadsCount { get; set; }

        [Required]
        public Plan NewPlan { get; set; }
    }
}
