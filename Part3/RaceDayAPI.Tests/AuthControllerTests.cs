using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RaceDayAPI.Controllers;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Helpers;
using Xunit;

namespace RaceDayAPI.Tests;

public class AuthControllerTests
{
    private const string AccessCode = "TestCode123";

    private static AuthController MakeController(RaceDayContext ctx, int? userId = null, string? role = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OrganiserAccessCode"] = AccessCode })
            .Build();

        return new AuthController(ctx, config).WithSession(userId, role);
    }

    private static RegisterRequest NewRequest(string role, string email = "new@test.com", string? code = null) => new()
    {
        Name = "Test User",
        Email = email,
        Password = "Password123",
        Role = role,
        OrganiserAccessCode = code
    };

    // ---------- registration ----------

    [Fact]
    public async Task Register_Participant_ReturnsCreated()
    {
        using var ctx = TestHelpers.NewContext();
        var controller = MakeController(ctx);

        var result = await controller.Register(NewRequest("Participant", "Sipho@Test.com"));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        var body = Assert.IsType<AuthResponse>(created.Value);
        Assert.Equal("Participant", body.Role);
        Assert.Equal("sipho@test.com", body.Email); // emails get saved in lowercase
        Assert.Equal(1, ctx.Participants.Count());
        Assert.Equal(0, ctx.Organisers.Count());
    }

    [Fact]
    public async Task Register_StoresHashedPasswordNotPlainText()
    {
        using var ctx = TestHelpers.NewContext();
        var controller = MakeController(ctx);

        await controller.Register(NewRequest("Participant"));

        var saved = ctx.Participants.Single();
        Assert.NotEqual("Password123", saved.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123", saved.PasswordHash));
    }

    [Fact]
    public async Task Register_Organiser_WithCorrectCode_ReturnsCreated()
    {
        using var ctx = TestHelpers.NewContext();
        var controller = MakeController(ctx);

        var result = await controller.Register(NewRequest("Organiser", "johan@test.com", AccessCode));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal("Organiser", Assert.IsType<AuthResponse>(created.Value).Role);
        Assert.Equal(1, ctx.Organisers.Count());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-code")]
    public async Task Register_Organiser_WithoutValidCode_ReturnsForbidden(string? code)
    {
        using var ctx = TestHelpers.NewContext();
        var controller = MakeController(ctx);

        var result = await controller.Register(NewRequest("Organiser", "johan@test.com", code));

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Equal(0, ctx.Organisers.Count());
    }

    [Fact]
    public async Task Register_SameEmailAsExistingParticipant_ReturnsConflict()
    {
        using var ctx = TestHelpers.NewContext();
        ctx.AddParticipant("sipho@test.com");
        var controller = MakeController(ctx);

        var result = await controller.Register(NewRequest("Participant", "sipho@test.com"));

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(1, ctx.Participants.Count());
    }

    // this is the "nobody can hold both roles" rule
    [Fact]
    public async Task Register_ParticipantEmailAsOrganiser_ReturnsConflict()
    {
        using var ctx = TestHelpers.NewContext();
        ctx.AddParticipant("sipho@test.com");
        var controller = MakeController(ctx);

        var result = await controller.Register(NewRequest("Organiser", "sipho@test.com", AccessCode));

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(0, ctx.Organisers.Count());
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        using var ctx = TestHelpers.NewContext();
        var controller = MakeController(ctx);

        var result = await controller.Register(NewRequest("Admin"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ---------- login ----------

    [Fact]
    public async Task Login_ValidParticipant_ReturnsOkAndStartsSession()
    {
        using var ctx = TestHelpers.NewContext();
        var participant = ctx.AddParticipant("sipho@test.com");
        var controller = MakeController(ctx);

        var result = await controller.Login(new LoginRequest { Email = "sipho@test.com", Password = "Password123" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Participant", Assert.IsType<AuthResponse>(ok.Value).Role);
        Assert.Equal((int?)participant.ParticipantId, controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Equal("Participant", controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task Login_ValidOrganiser_ReturnsOkAndStartsSession()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser("johan@test.com");
        var controller = MakeController(ctx);

        var result = await controller.Login(new LoginRequest { Email = "johan@test.com", Password = "Password123" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Organiser", Assert.IsType<AuthResponse>(ok.Value).Role);
        Assert.Equal((int?)organiser.OrganiserId, controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Equal("Organiser", controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task Login_EmailIsNotCaseSensitive()
    {
        using var ctx = TestHelpers.NewContext();
        ctx.AddParticipant("sipho@test.com");
        var controller = MakeController(ctx);

        var result = await controller.Login(new LoginRequest { Email = "SIPHO@TEST.COM", Password = "Password123" });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorizedAndNoSession()
    {
        using var ctx = TestHelpers.NewContext();
        ctx.AddParticipant("sipho@test.com");
        var controller = MakeController(ctx);

        var result = await controller.Login(new LoginRequest { Email = "sipho@test.com", Password = "WrongPass1" });

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Null(controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        using var ctx = TestHelpers.NewContext();
        var controller = MakeController(ctx);

        var result = await controller.Login(new LoginRequest { Email = "nobody@test.com", Password = "Password123" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ---------- logout ----------

    [Fact]
    public void Logout_ClearsTheSession()
    {
        using var ctx = TestHelpers.NewContext();
        var controller = MakeController(ctx, 1, Roles.Participant);

        var result = controller.Logout();

        Assert.IsType<OkObjectResult>(result);
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Null(controller.HttpContext.Session.GetString(SessionKeys.Role));
    }
}