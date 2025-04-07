using Moq;
using Drawsome.Hubs;
using Microsoft.AspNetCore.SignalR;
using Drawsome.Models;

namespace Drawsome.Tests;

[TestFixture]
public class LobbyHubTests
{
    private Mock<IHubCallerClients> _mockClients;
    private Mock<IGroupManager> _mockGroups;
    private Mock<HubCallerContext> _mockContext;
    private LobbyHub _hub;

    [SetUp]
    public void Setup()
    {
        _mockClients = new Mock<IHubCallerClients>();
        _mockGroups = new Mock<IGroupManager>();
        _mockContext = new Mock<HubCallerContext>();

        // When Clients.Group() is called with any string, return a mock IClientProxy.
        _mockClients.Setup(clients => clients.Group(It.IsAny<string>()))
            .Returns(new Mock<IClientProxy>().Object);

        // When Clients.Caller is accessed, return a mock ISingleClientProxy.
        _mockClients.Setup(clients => clients.Caller)
            .Returns(new Mock<ISingleClientProxy>().Object);

        // Create the hub instance with the mocked dependencies
        _hub = new LobbyHub()
        {
            Clients = _mockClients.Object,
            Groups = _mockGroups.Object,
            Context = _mockContext.Object
        };

        // Make sure to clear the Lobbies dictionary before each test
        LobbyHub.Lobbies.Clear();
    }

    [Test]
    public async Task SendDrawing_SendsDrawingDataToCorrectLobbyGroup()
    {
        // Arrange
        const string lobbyName = "TestLobby";
        const string drawingData = "some drawing information";
        var mockGroupProxy = new Mock<IClientProxy>();
        _mockClients.Setup(clients => clients.Group(lobbyName)).Returns(mockGroupProxy.Object);

        // Act
        await _hub.SendDrawing(lobbyName, drawingData);

        // Assert
        mockGroupProxy.Verify(proxy => proxy.SendCoreAsync(
                "ReceiveDrawing",
                It.Is<object[]>(args => args.Length == 1 && args[0].Equals(drawingData)),
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task CreateLobby_NewLobbyName_CreatesLobbyAndJoinsGroup()
    {
        // Arrange
        const string lobbyName = "NewLobby";
        const string username = "TestUser";
        var mockGroupProxy = new Mock<IClientProxy>();
        _mockClients.Setup(clients => clients.Group(lobbyName)).Returns(mockGroupProxy.Object);
        _mockContext.Setup(context => context.ConnectionId).Returns("testConnectionId");

        // Act
        var result = await _hub.CreateLobby(lobbyName, username);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(LobbyHub.Lobbies.ContainsKey(lobbyName), Is.True);
        });
        _mockGroups.Verify(groups => groups.AddToGroupAsync("testConnectionId", lobbyName, CancellationToken.None), Times.Once);
        mockGroupProxy.Verify(proxy => proxy.SendCoreAsync(
                "UpdatePlayers",
                It.Is<object[]>(args => args.Length == 1 && ((args[0] as List<string>)!).Contains(username) == true),
                CancellationToken.None),
            Times.Once);
        Assert.That(LobbyHub.Lobbies[lobbyName]?.Players, Contains.Item(username));
    }

    [Test]
    public async Task CreateLobby_ExistingLobbyName_DoesNotCreateLobbyAndNotifiesCaller()
    {
        // Arrange
        const string lobbyName = "ExistingLobby";
        const string username = "TestUser";
        LobbyHub.Lobbies.TryAdd(lobbyName, new Lobby { LobbyName = lobbyName });
        var mockCaller = new Mock<ISingleClientProxy>();
        _mockClients.Setup(clients => clients.Caller).Returns(mockCaller.Object);

        // Act
        var result = await _hub.CreateLobby(lobbyName, username);

        // Assert
        Assert.That(result, Is.False);
        mockCaller.Verify(caller => caller.SendCoreAsync(
                "LobbyCreationFailed",
                It.Is<object[]>(args => args.Length == 1 && args[0].Equals(lobbyName)),
                CancellationToken.None),
            Times.Once);
        Assert.That(LobbyHub.Lobbies, Has.Count.EqualTo(1)); // Ensure no new lobby was added
        _mockGroups.Verify(groups => groups.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None), Times.Never);
    }

    [Test]
    public async Task JoinLobby_ExistingLobby_UserNotExists_JoinsLobbyAndUpdatesPlayers()
    {
        // Arrange
        const string lobbyName = "ExistingLobby";
        const string joiningUser = "NewPlayer";
        var lobby = new Lobby { LobbyName = lobbyName, Players = ["Player1"] };
        LobbyHub.Lobbies.TryAdd(lobbyName, lobby);
        var mockGroupProxy = new Mock<IClientProxy>();
        _mockClients.Setup(clients => clients.Group(lobbyName)).Returns(mockGroupProxy.Object);
        _mockContext.Setup(context => context.ConnectionId).Returns("testConnectionId"); // Ensure ConnectionId is set

        // Act
        var result = await _hub.JoinLobby(lobbyName, joiningUser);

        // Assert
        Assert.IsTrue(result);
        Assert.That(lobby.Players, Contains.Item(joiningUser));
        _mockGroups.Verify(groups => groups.AddToGroupAsync("testConnectionId", lobbyName, CancellationToken.None), Times.Once);
        mockGroupProxy.Verify(proxy => proxy.SendCoreAsync(
                "UpdatePlayers",
                It.Is<object[]>(args => ((args[0] as List<string>)!).Contains(joiningUser) == true && ((args[0] as List<string>)!).Contains("Player1") == true && ((args[0] as List<string>)!).Count == 2),
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task JoinLobby_ExistingLobby_UserAlreadyExists_DoesNotAddUserAgain()
    {
        // Arrange
        const string lobbyName = "ExistingLobby";
        const string existingUser = "ExistingPlayer";
        var lobby = new Lobby { LobbyName = lobbyName, Players = [existingUser] };
        LobbyHub.Lobbies.TryAdd(lobbyName, lobby);
        var mockGroupProxy = new Mock<IClientProxy>();
        _mockClients.Setup(clients => clients.Group(lobbyName)).Returns(mockGroupProxy.Object);

        // Act
        var result = await _hub.JoinLobby(lobbyName, existingUser);

        // Assert
        Assert.IsTrue(result);
        Assert.That(lobby.Players.Count, Is.EqualTo(1));
        Assert.That(lobby.Players, Contains.Item(existingUser));
        _mockGroups.Verify(groups => groups.AddToGroupAsync("testConnectionId", lobbyName, CancellationToken.None), Times.Never);
        mockGroupProxy.Verify(proxy => proxy.SendCoreAsync(
            "UpdatePlayers",
            It.Is<object[]>(args => args.Length == 1 && ((args[0] as List<string>)!).Count == 1 && ((args[0] as List<string>)!).Contains(existingUser) == true),
            CancellationToken.None),
        Times.Once);
    }

    [Test]
    public async Task JoinLobby_NonExistingLobby_ReturnsFalseAndNotifiesCaller()
    {
        // Arrange
        const string nonExistingLobby = "NonExistent";
        const string joiningUser = "SomePlayer";
        var mockCallerProxy = new Mock<ISingleClientProxy>();
        _mockClients.Setup(clients => clients.Caller).Returns(mockCallerProxy.Object);

        // Act
        var result = await _hub.JoinLobby(nonExistingLobby, joiningUser);

        // Assert
        Assert.IsFalse(result);
        mockCallerProxy.Verify(proxy => proxy.SendCoreAsync(
            "LobbyNotFound",
            It.Is<object[]>(args => args.Length == 1 && args[0].Equals(nonExistingLobby)),
            CancellationToken.None),
        Times.Once);
        _mockGroups.Verify(groups => groups.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task LeaveLobby_ExistingLobby_UserExists_LeavesLobbyAndUpdatesPlayers()
    {
        // Arrange
        const string lobbyName = "ExistingLobby";
        const string leavingUser = "LeavingPlayer";
        var lobby = new Lobby { LobbyName = lobbyName, Players = ["Player1", leavingUser, "Player2"] };
        LobbyHub.Lobbies.TryAdd(lobbyName, lobby);
        var mockGroupProxy = new Mock<IClientProxy>();
        _mockClients.Setup(clients => clients.Group(lobbyName)).Returns(mockGroupProxy.Object);
        _mockContext.Setup(context => context.ConnectionId).Returns("testConnectionId"); // Ensure ConnectionId is set

        // Act
        await _hub.LeaveLobby(lobbyName, leavingUser);

        // Assert
        Assert.That(lobby.Players, Does.Not.Contain(leavingUser));
        Assert.That(lobby.Players.Count, Is.EqualTo(2));
        _mockGroups.Verify(groups => groups.RemoveFromGroupAsync("testConnectionId", lobbyName, CancellationToken.None), Times.Once);
        mockGroupProxy.Verify(proxy => proxy.SendCoreAsync(
                "UpdatePlayers",
                It.Is<object[]>(args => args.Length == 1 && ((args[0] as List<string>)!).Count == 2 && !((args[0] as List<string>)!).Contains(leavingUser) == true),
                CancellationToken.None),
            Times.Once);
        Assert.That(LobbyHub.Lobbies.ContainsKey(lobbyName), Is.True); // Lobby should still exist
    }

    [Test]
    public async Task LeaveLobby_ExistingLobby_UserDoesNotExist_DoesNothing()
    {
        // Arrange
        const string lobbyName = "ExistingLobby";
        const string nonExistingUser = "NonExistent";
        var lobby = new Lobby { LobbyName = lobbyName, Players = ["Player1"] };
        LobbyHub.Lobbies.TryAdd(lobbyName, lobby);
        var mockGroupProxy = new Mock<IClientProxy>();
        _mockClients.Setup(clients => clients.Group(lobbyName)).Returns(mockGroupProxy.Object);

        // Act
        await _hub.LeaveLobby(lobbyName, nonExistingUser);

        // Assert
        Assert.That(lobby.Players.Count, Is.EqualTo(1));
        Assert.That(lobby.Players, Contains.Item("Player1"));
        _mockGroups.Verify(groups => groups.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        mockGroupProxy.Verify(proxy => proxy.SendCoreAsync(
            "UpdatePlayers",
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()),
        Times.Never); // No update should be sent
        Assert.That(LobbyHub.Lobbies.ContainsKey(lobbyName), Is.True); // Lobby should still exist
    }

    [Test]
    public async Task LeaveLobby_ExistingLobby_LastUserLeaves_LobbyIsRemoved()
    {
        // Arrange
        const string lobbyName = "ExistingLobby";
        const string leavingUser = "LonelyPlayer";
        var lobby = new Lobby { LobbyName = lobbyName, Players = [leavingUser] };
        LobbyHub.Lobbies.TryAdd(lobbyName, lobby);
        _mockContext.Setup(context => context.ConnectionId).Returns("testConnectionId"); // Ensure ConnectionId is set

        // Act
        await _hub.LeaveLobby(lobbyName, leavingUser);

        // Assert
        Assert.That(LobbyHub.Lobbies.ContainsKey(lobbyName), Is.False);
        _mockGroups.Verify(groups => groups.RemoveFromGroupAsync("testConnectionId", lobbyName, CancellationToken.None), Times.Once);
        _mockClients.Verify(clients => clients.Group(lobbyName).SendCoreAsync(
                "UpdatePlayers",
                It.Is<object[]>(args => args.Length == 1 && ((args[0] as List<string>)!).Count == 0),
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task LeaveLobby_NonExistingLobby_DoesNothing()
    {
        // Arrange
        const string nonExistingLobby = "NonExistent";
        const string leavingUser = "SomePlayer";

        // Act
        await _hub.LeaveLobby(nonExistingLobby, leavingUser);

        // Assert
        _mockGroups.Verify(groups => groups.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockClients.Verify(clients => clients.Group(It.IsAny<string>()).SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()),
        Times.Never);
    }
}