using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.DTOs;
using RaceDayAPI.Helpers;
using Xunit;

namespace RaceDayAPI.Tests;

public class ProfileControllerTests
{
    [Fact]
    public async Task GetParticipant_ReturnsOwnProfile()
    {
        using var ctx = TestHelpers.NewContext();
        var participant = ctx.AddParticipant("sipho@test.com");
        var controller = new ProfileController(ctx).WithSession(participant.ParticipantId, Roles.Participant);

        var ok = Assert.IsType<OkObjectResult>(await controller.GetParticipant());

        var body = Assert.IsType<ProfileResponse>(ok.Value);
        Assert.Equal("sipho@test.com", body.Email);
        Assert.Equal("Participant", body.Role);
    }

    [Fact]
    public async Task UpdateParticipant_ChangesNameAndEmail()
    {
        using var ctx = TestHelpers.NewContext();
        var participant = ctx.AddParticipant("sipho@test.com");
        var controller = new ProfileController(ctx).WithSession(participant.ParticipantId, Roles.Participant);

        var result = await controller.UpdateParticipant(new ProfileRequest { Name = "Sipho N", Email = "New@Test.com" });

        Assert.IsType<OkObjectResult>(result);
        var saved = ctx.Participants.Single();
        Assert.Equal("Sipho N", saved.Name);
        Assert.Equal("new@test.com", saved.Email);
    }

    [Fact]
    public async Task UpdateParticipant_EmailUsedByAnOrganiser_ReturnsConflict()
    {
        using var ctx = TestHelpers.NewContext();
        ctx.AddOrganiser("johan@test.com");
        var participant = ctx.AddParticipant("sipho@test.com");
        var controller = new ProfileController(ctx).WithSession(participant.ParticipantId, Roles.Participant);

        var result = await controller.UpdateParticipant(new ProfileRequest { Name = "Sipho", Email = "johan@test.com" });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("sipho@test.com", ctx.Participants.Single().Email);
    }

    [Fact]
    public async Task GetOrganiser_ReturnsOwnProfile()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser("johan@test.com");
        var controller = new ProfileController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var ok = Assert.IsType<OkObjectResult>(await controller.GetOrganiser());

        var body = Assert.IsType<ProfileResponse>(ok.Value);
        Assert.Equal("johan@test.com", body.Email);
        Assert.Equal("Organiser", body.Role);
    }
}