using AxertLMS.Models;
using AxertLMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace AxertLMS.Controllers
{
    public class ParaphraserController : Controller
    {
        private readonly ParaphraserService _paraphraserService;

        public ParaphraserController()
        {
            _paraphraserService = new ParaphraserService();
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!IsSignedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            return View(new ParaphraserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(ParaphraserViewModel model)
        {
            if (!IsSignedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            string[] allowedStyles =
            {
                "Standard",
                "Formal",
                "Simpler"
            };

            if (!allowedStyles.Contains(model.Style))
            {
                ModelState.AddModelError(
                    nameof(model.Style),
                    "Please select a valid paraphrasing style.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Result = _paraphraserService.Paraphrase(
                model.Text,
                model.Style);

            return View(model);
        }

        private bool IsSignedIn()
        {
            return HttpContext.Session.GetInt32("UserId").HasValue
                && !string.IsNullOrWhiteSpace(
                    HttpContext.Session.GetString("UserRole"));
        }
    }
}