using Microsoft.AspNetCore.Mvc;
using Moq;
using Drawsome.Controllers;
using Drawsome.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Drawsome.Hubs;
using System.Collections.Concurrent;
using System.Diagnostics;
using Drawsome.Models;
using Microsoft.EntityFrameworkCore;

namespace Drawsome.Tests;

[TestFixture]
public class HomeControllerTests
{
    private Mock<ApplicationDbContext> _mockContext;
    private HomeController? _controller;
    private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private Mock<ISession> _mockSession;
    private Mock<ITempDataDictionary> _mockTempData;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _mockContext = new Mock<ApplicationDbContext>(options);
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockSession = new Mock<ISession>();
        _mockTempData = new Mock<ITempDataDictionary>();

        _mockHttpContextAccessor.Setup(x => x.HttpContext!.Session).Returns(_mockSession.Object);

        if (_mockHttpContextAccessor.Object.HttpContext != null)
            _controller = new HomeController(_mockContext.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = _mockHttpContextAccessor.Object.HttpContext
                },
                TempData = _mockTempData.Object
            };
    }

   [Test]
    public void Login_ValidUser_RedirectsToLobbySelection()
    {
        // Arrange
        var user = new User { Username = "testuser", Password = BCrypt.Net.BCrypt.HashPassword("password") };
        var users = new List<User> { user }.AsQueryable(); // Create a queryable list of users.

        // Create a mock DbSet using the Moq.EntityFrameworkCore extension.
        var mockSet = new Mock<DbSet<User>>();
        mockSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(users.Provider);
        mockSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
        mockSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
        using var enumerator = users.GetEnumerator();
        mockSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(enumerator);

        _mockContext.Setup(x => x.Users).Returns(mockSet.Object); // Set up the Users DbSet to return the mock.

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.Login("testuser", "password") as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("LobbySelection"));
    }

    [Test]
    public void Login_InvalidUser_ReturnsViewWithModelError()
    {
        // Arrange
        var users = new List<User>().AsQueryable(); // Create an empty queryable list of users.

        // Create a mock DbSet using the Moq.EntityFrameworkCore extension.
        var mockSet = new Mock<DbSet<User>>();
        mockSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(users.Provider);
        mockSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
        mockSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
        using var enumerator = users.GetEnumerator();
        mockSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(enumerator);

        _mockContext.Setup(x => x.Users).Returns(mockSet.Object); // Set up the Users DbSet to return the mock.

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.Login("testuser", "password") as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(_controller.ModelState.IsValid, Is.False);
    }
        [Test]
    public void LobbySelection_UsernameInSession_ReturnsView()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue("Username", out It.Ref<byte[]>.IsAny!))
            .Returns(true)
            .Callback((string _, out byte[] val) => {
                val = "testuser"u8.ToArray();
            });

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.LobbySelection() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void LobbySelection_UsernameNotInSession_RedirectsToIndex()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny!))
            .Returns(false);

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.LobbySelection() as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("Index"));
    }

    [Test]
    public void ActiveLobby_UsernameNotInSession_RedirectsToIndex()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny!))
            .Returns(false);

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.ActiveLobby("TestLobby") as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("Index"));
    }

    [Test]
    public void ActiveLobby_LobbyNameIsNullOrEmpty_RedirectsToLobbySelection()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue("Username", out It.Ref<byte[]>.IsAny!))
            .Returns(true)
            .Callback((string _, out byte[] val) => {
                val = "testuser"u8.ToArray();
            });

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.ActiveLobby(null!) as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("LobbySelection"));
    }

    [Test]
    public void ActiveLobby_LobbyDoesNotExist_RedirectsToLobbySelection()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue("Username", out It.Ref<byte[]>.IsAny!))
            .Returns(true)
            .Callback((string _, out byte[] val) => {
                val = "testuser"u8.ToArray();
            });

        LobbyHub.Lobbies = new ConcurrentDictionary<string, Lobby>();

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.ActiveLobby("NonExistentLobby") as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("LobbySelection"));
    }

    [Test]
    public void ActiveLobby_ValidLobby_ReturnsView()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue("Username", out It.Ref<byte[]>.IsAny!))
            .Returns(true)
            .Callback((string _, out byte[] val) => {
                val = "testuser"u8.ToArray();
            });

        LobbyHub.Lobbies = new ConcurrentDictionary<string, Lobby>();
        LobbyHub.Lobbies.TryAdd("TestLobby", new Lobby() { Players = [] });

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.ActiveLobby("TestLobby") as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void ManageUsers_UsernameNotInSession_RedirectsToIndex()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny!))
            .Returns(false);

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.ManageUsers() as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("Index"));
    }

    [Test]
    public void ManageUsers_UserIsNotAdmin_RedirectsToLobbySelection()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue("Username", out It.Ref<byte[]>.IsAny!))
            .Returns(true)
            .Callback((string _, out byte[] val) => { val = "testuser"u8.ToArray(); });

        var user = new User { Username = "testuser", IsAdmin = false };
        _mockContext.Setup(c => c.Users).Returns(MockDbSet([user]));

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.ManageUsers() as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("LobbySelection"));
    }

    [Test]
    public void ManageUsers_UserIsAdmin_ReturnsViewWithUsers()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue("Username", out It.Ref<byte[]>.IsAny!))
            .Returns(true)
            .Callback((string _, out byte[] val) => { val = "testuser"u8.ToArray(); });

        var adminUser = new User { Username = "testuser", IsAdmin = true };
        var otherUsers = new List<User> { new User { Username = "user1" }, new User { Username = "user2" } };
        var allUsers = new List<User> { adminUser }.Concat(otherUsers).ToList();

        _mockContext.Setup(c => c.Users).Returns(MockDbSet(allUsers));

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.ManageUsers() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Model, Is.EqualTo(allUsers));
    }

    [Test]
    public void EditUser_UsernameNotInSession_RedirectsToIndex()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny!))
            .Returns(false);

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.EditUser(1) as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("Index"));
    }

    [Test]
    public void EditUser_UserIsNotAdmin_RedirectsToLobbySelection()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue("Username", out It.Ref<byte[]>.IsAny!))
            .Returns(true)
            .Callback((string _, out byte[] val) => { val = "testuser"u8.ToArray(); });

        var user = new User { Username = "testuser", IsAdmin = false };
        _mockContext.Setup(c => c.Users).Returns(MockDbSet([user]));

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.EditUser(1) as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("LobbySelection"));
    }

    [Test]
    public void EditUser_UserIsAdmin_ReturnsViewWithUserToEdit()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue("Username", out It.Ref<byte[]>.IsAny!))
            .Returns(true)
            .Callback((string _, out byte[] val) => { val = "testuser"u8.ToArray(); });

        var adminUser = new User { Username = "testuser", IsAdmin = true };
        var userToEdit = new User { Id = 1, Username = "edituser" };
        var allUsers = new List<User> { adminUser, userToEdit };

        _mockContext.Setup(c => c.Users).Returns(MockDbSet(allUsers));

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.EditUser(1) as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Model, Is.EqualTo(userToEdit));
    }
    
    [Test]
    public void UpdateUser_UsernameNotInSession_RedirectsToIndex()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny!))
            .Returns(false);

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.UpdateUser(1, "newuser", "newpass", 100, true) as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("Index"));
    }

    [Test]
    public void UpdateUser_UserIsNotAdmin_RedirectsToLobbySelection()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue("Username", out It.Ref<byte[]>.IsAny!))
            .Returns(true)
            .Callback((string _, out byte[] val) => {
                val = "testuser"u8.ToArray();
            });

        var user = new User { Username = "testuser", IsAdmin = false };
        _mockContext.Setup(c => c.Users).Returns(MockDbSet([user]));

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.UpdateUser(1, "newuser", "newpass", 100, true) as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("LobbySelection"));
    }

    [Test]
    public void UpdateUser_ValidUpdate_RedirectsToManageUsers()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue("Username", out It.Ref<byte[]>.IsAny!))
            .Returns(true)
            .Callback((string _, out byte[] val) => {
                val = "testuser"u8.ToArray();
            });

        var adminUser = new User { Username = "testuser", IsAdmin = true };
        var userToUpdate = new User { Id = 1, Username = "olduser", Score = 50, IsAdmin = false };
        _mockContext.Setup(c => c.Users).Returns(MockDbSet([adminUser, userToUpdate]));

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.UpdateUser(1, "newuser", "newpass", 100, true) as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.ActionName, Is.EqualTo("ManageUsers"));
            Assert.That(userToUpdate.Username, Is.EqualTo("newuser"));
            Assert.That(userToUpdate.Score, Is.EqualTo(100));
            Assert.That(userToUpdate.IsAdmin, Is.True);
        });
    }

    [Test]
    public void DeleteUser_UsernameNotInSession_RedirectsToIndex()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny!))
            .Returns(false);

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.DeleteUser(1) as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("Index"));
    }

    [Test]
    public void DeleteUser_UserIsNotAdmin_RedirectsToLobbySelection()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue("Username", out It.Ref<byte[]>.IsAny!))
            .Returns(true)
            .Callback((string _, out byte[] val) => {
                val = "testuser"u8.ToArray();
            });

        var user = new User { Username = "testuser", IsAdmin = false };
        _mockContext.Setup(c => c.Users).Returns(MockDbSet([user]));

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.DeleteUser(1) as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("LobbySelection"));
    }

    [Test]
    public void DeleteUser_ValidDelete_RedirectsToManageUsers()
    {
        // Arrange
        _mockSession.Setup(s => s.TryGetValue("Username", out It.Ref<byte[]>.IsAny!))
            .Returns(true)
            .Callback((string _, out byte[] val) => {
                val = "testuser"u8.ToArray();
            });

        var adminUser = new User { Username = "testuser", IsAdmin = true };
        var userToDelete = new User { Id = 1, Username = "deleteuser" };
        var allUsers = new List<User> { adminUser, userToDelete };
        _mockContext.Setup(c => c.Users).Returns(MockDbSet(allUsers));

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.DeleteUser(1) as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("ManageUsers"));
        _mockContext.Verify(c => c.Users.Remove(userToDelete), Times.Once);
        _mockContext.Verify(c => c.SaveChanges(), Times.Once);
    }
    
    [Test]
    public void Logout_ClearsSession_RedirectsToIndex()
    {
        // Arrange
        _mockSession.Setup(s => s.Clear());

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.Logout() as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("Index"));
        _mockSession.Verify(s => s.Clear(), Times.Once);
    }

    [Test]
    public void Logout_RedirectsToIndex()
    {
        // Arrange (Nothing to arrange specifically, as we only need to test the redirect)

        // Act
        Debug.Assert(_controller != null, nameof(_controller) + " != null");
        var result = _controller.Logout() as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ActionName, Is.EqualTo("Index"));
    }

    private static DbSet<T> MockDbSet<T>(List<T> data) where T : class
    {
        var queryableData = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryableData.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryableData.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryableData.ElementType);
        using var enumerator = queryableData.GetEnumerator();
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(enumerator);
        return mockSet.Object;
    }
}