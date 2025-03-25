using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Drawsome.Models;
using Drawsome.Data;
using System.Linq;
using BCrypt.Net;

namespace Drawsome.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

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
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError("", "Username and password are required.");
            return View();
        }

        if (_context.Users.Any(u => u.Username == username))
        {
            ModelState.AddModelError("", "Username already exists.");
            return View();
        }
        
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var newUser = new User
        {
            Username = username,
            Password = hashedPassword,
            Score = 0,
            IsAdmin = false
        };

        _context.Users.Add(newUser);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}