using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Drawsome.Models;
using Drawsome.Data;

namespace Drawsome.Controllers;

public class HomeController(ApplicationDbContext context) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(string username, string password)
    {
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var newUser = new User
        {
            Username = username,
            Password = hashedPassword,
            Score = 0,
            IsAdmin = false
        };

        context.Users.Add(newUser);
        context.SaveChanges();

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        var user = context.Users.FirstOrDefault(u => u.Username == username);

        if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            HttpContext.Session.SetString("Username", user.Username);
            return RedirectToAction("LobbySelection");
        }
        else
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View("Index");
        }
    }

    public IActionResult LobbySelection()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
        {
            return RedirectToAction("Index");
        }
        return View();
    }

    public IActionResult ActiveLobby(string lobbyName)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
        {
            return RedirectToAction("Index");
        }

        if (string.IsNullOrEmpty(lobbyName))
        {
            return RedirectToAction("LobbySelection");
        }

        ViewBag.LobbyName = lobbyName;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}