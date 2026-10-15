using BookLibrary.Data;
using BookLibrary.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookLibrary.Controllers
{
    public class AccountController : Controller
    {
        private readonly IDatabase db;

        public AccountController(IDatabase db)
        {
            this.db = db;
        }

        // LOGIN - GET
        public IActionResult Login()
        {
            return View();
        }

        // LOGIN - POST
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = db.AuthenticateUser(username, password);
            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);

                return RedirectToAction("Index", "Books");
            }

            ModelState.AddModelError("", "Ггрешно име или парола.");
            return View();
        }

        // REGISTER - GET
        public IActionResult Register()
        {
            return View();
        }

        // REGISTER - POST
        [HttpPost]
        public IActionResult Register(string username, string password)
        {
            var user = db.RegisterUser(username, password);
            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);

                return RedirectToAction("Index", "Books");
            }

            ModelState.AddModelError("Username", "Потребителското име вече е заето.");
            return View();
        }

        // LOGOUT
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
