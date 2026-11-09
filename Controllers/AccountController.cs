using BookLibrary.Data;
using BookLibrary.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookLibrary.Controllers
{
    public class AccountController : Controller
    {
        private readonly IDatabase db;

        //Dependency Injection към базата данни
        public AccountController(IDatabase db)
        {
            this.db = db;
        }

        // LOGIN - GET
        //връща страница за вход
        public IActionResult Login()
        {
            return View();
        }

        // LOGIN - POST
        //извиква се след изпращане на формата
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Моля, попълнете всички полета.");
                return View();
            }
            
            var user = db.AuthenticateUser(username, password);
            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);

                return RedirectToAction("Index", "Books");
            }

            ModelState.AddModelError("", "Ггрешно потребителско име или парола.");
            return View();
        }

        // REGISTER - GET
        //връща страница за регистрация
        public IActionResult Register()
        {
            return View();
        }

        // REGISTER - POST
        //извиква се след изпращане на формата
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User model)
        {
            // Проверяваме дали моделът отговаря на изискванията в User.cs
            if (ModelState.IsValid)
            {
                var registeredUser = db.RegisterUser(model.Username, model.Password);
                if (registeredUser != null)
                {
                    TempData["SuccessMessage"] = "Успешна регистрация! Моля, влезте в профила си.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("Username", "Потребителското име вече е заето.");
            }

            return View(model);
        }

        // LOGOUT
        //изчиства сесията
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
