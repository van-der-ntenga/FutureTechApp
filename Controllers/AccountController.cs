using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FutureTechApp.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public IActionResult Login(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null)
        {
            var info = await HttpContext.AuthenticateAsync();

            // List of allowed admin emails
            var allowedAdminEmails = new List<string>
            {
                "sbongagezzy0630@gmail.com",
                "thabisoawande033@gmail.com",
                "noreply.deloslounge@gmail.com",
                "ntwenhleqwabe@gmail.com",
                "siyabulelanjaju02@gmail.com",
                "khozabayanda50@gmail.com",
                "lindelwasibiya624@gmail.com",
                "smnguni217@gmail.com",
                "snenhlanhla.nosiphom@gmail.com",
                "phindilenothy@gmail.com"
            };

            var userEmail = info.Principal?.FindFirstValue(ClaimTypes.Email);

            // Check if the email is in the list of allowed admin emails
            if (allowedAdminEmails.Contains(userEmail))
            {
                // Add claims for admin
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, info.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty),
                    new Claim(ClaimTypes.Name, info.Principal?.FindFirstValue(ClaimTypes.Name) ?? string.Empty),
                    new Claim(ClaimTypes.Email, userEmail),
                    new Claim(ClaimTypes.Role, "Admin") // Add role claim for admin
                };

                var claimsIdentity = new ClaimsIdentity(claims, "External");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(claimsPrincipal);

                return LocalRedirect(returnUrl ?? "/Home/Dashboard");
            }
            else
            {
                // If the email is not allowed, redirect to an error page or deny access
                return RedirectToAction("Error", new { message = "You do not have access to this application." });
            }
        }

        [HttpPost]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Error(string message)
        {
            ViewBag.ErrorMessage = message;
            return View();
        }
    }
}