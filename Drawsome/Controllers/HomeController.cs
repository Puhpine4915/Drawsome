using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Drawsome.Models;
using Drawsome.Data;
using Drawsome.Hubs;

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
        
        if (!LobbyHub.Lobbies.ContainsKey(lobbyName))
        {
            return RedirectToAction("LobbySelection");
        }

        ViewBag.LobbyName = lobbyName;
        return View();
    }
    
    public IActionResult ManageUsers()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
        {
            return RedirectToAction("Index");
        }

        var username = HttpContext.Session.GetString("Username");
        var user = context.Users.FirstOrDefault(u => u.Username == username);

        if (user == null || !user.IsAdmin)
        {
            return RedirectToAction("LobbySelection");
        }

        var users = context.Users.ToList();
        return View(users);
    }

    public IActionResult EditUser(int id)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
        {
            return RedirectToAction("Index");
        }

        var username = HttpContext.Session.GetString("Username");
        var user = context.Users.FirstOrDefault(u => u.Username == username);

        if (user == null || !user.IsAdmin)
        {
            return RedirectToAction("LobbySelection");
        }

        var userToEdit = context.Users.FirstOrDefault(u => u.Id == id);
        return View(userToEdit);
    }

    [HttpPost]
    public IActionResult UpdateUser(int id, string username, string password, int score, bool isAdmin)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
        {
            return RedirectToAction("Index");
        }

        var adminUser = context.Users.FirstOrDefault(u => u.Username == HttpContext.Session.GetString("Username"));

        if (adminUser == null || !adminUser.IsAdmin)
        {
            return RedirectToAction("LobbySelection");
        }

        var userToUpdate = context.Users.FirstOrDefault(u => u.Id == id);
        if (userToUpdate != null)
        {
            userToUpdate.Username = username;
            if (!string.IsNullOrEmpty(password))
            {
                userToUpdate.Password = BCrypt.Net.BCrypt.HashPassword(password);
            }
            userToUpdate.Score = score;
            userToUpdate.IsAdmin = isAdmin;
            context.SaveChanges();
        }

        return RedirectToAction("ManageUsers");
    }
    
    public IActionResult DeleteUser(int id)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
        {
            return RedirectToAction("Index");
        }

        var adminUser = context.Users.FirstOrDefault(u => u.Username == HttpContext.Session.GetString("Username"));

        if (adminUser == null || !adminUser.IsAdmin)
        {
            return RedirectToAction("LobbySelection"); // Only admins can access
        }

        var userToDelete = context.Users.FirstOrDefault(u => u.Id == id);
        if (userToDelete != null)
        {
            context.Users.Remove(userToDelete);
            context.SaveChanges();
        }

        return RedirectToAction("ManageUsers");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}