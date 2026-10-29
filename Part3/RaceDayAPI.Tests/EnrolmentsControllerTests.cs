using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.DTOs;
using RaceDayAPI.Helpers;
using Xunit;

namespace RaceDayAPI.Tests;

public class EnrolmentsControllerTests
{
    [Fact]
    public async Task Enrol_AsParticipant_RecordsParticipantCategoryAndEvent()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var participant = ctx.AddParticipant();
        var ev = ctx.AddEvent(organiser.OrganiserId);
        var category = ctx.AddCategory(ev.EventId);
        var controller = new EnrolmentsController(ctx).WithSession(participant.ParticipantId, Roles.Participant);

        var result = await controller.Enrol(new EnrolmentRequest { CategoryId = category.CategoryId });

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        var body = Assert.IsType<EnrolmentResponse>(created.Value);
        Assert.Equal(participant.ParticipantId, body.ParticipantId);
        Assert.Equal(category.CategoryId, body.CategoryId);
        Assert.Equal(ev.EventId, body.EventId); // event is linked through the category

        // and it's really saved in the database
        var saved = ctx.Enrolments.Single();
        Assert.Equal(participant.ParticipantId, saved.ParticipantId);
        Assert.Equal(category.CategoryId, saved.CategoryId);
        Assert.Equal("Confirmed", saved.Status);
    }

    [Fact]
    public async Task Enrol_InSameCategoryTwice_ReturnsConflict()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var participant = ctx.AddParticipant();
        var ev = ctx.AddEvent(organiser.OrganiserId);
        var category = ctx.AddCategory(ev.EventId);
        ctx.AddEnrolment(participant.ParticipantId, category.CategoryId);
        var controller = new EnrolmentsController(ctx).WithSession(participant.ParticipantId, Roles.Participant);

        var result = await controller.Enrol(new EnrolmentRequest { CategoryId = category.CategoryId });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(1, ctx.Enrolments.Count());
    }

    [Fact]
    public async Task Enrol_UnknownCategory_ReturnsNotFound()
    {
        using var ctx = TestHelpers.NewContext();
        var participant = ctx.AddParticipant();
        var controller = new EnrolmentsController(ctx).WithSession(participant.ParticipantId, Roles.Participant);

        var result = await controller.Enrol(new EnrolmentRequest { CategoryId = 999 });

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(0, ctx.Enrolments.Count());
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyTheLoggedInParticipantsEnrolments()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var sipho = ctx.AddParticipant("sipho@test.com");
        var anrich = ctx.AddParticipant("anrich@test.com", "Anrich Botha");
        var ev = ctx.AddEvent(organiser.OrganiserId);
        var category = ctx.AddCategory(ev.EventId);
        ctx.AddEnrolment(sipho.ParticipantId, category.CategoryId);
        ctx.AddEnrolment(anrich.ParticipantId, category.CategoryId);
        var controller = new EnrolmentsController(ctx).WithSession(sipho.ParticipantId, Roles.Participant);

        var ok = Assert.IsType<OkObjectResult>(await controller.GetMine());

        var list = Assert.IsAssignableFrom<IEnumerable<EnrolmentResponse>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal(sipho.ParticipantId, list[0].ParticipantId);
    }

    [Fact]
    public async Task GetForEvent_ByOwningOrganiser_ReturnsEnrolments()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var participant = ctx.AddParticipant();
        var ev = ctx.AddEvent(organiser.OrganiserId);
        var category = ctx.AddCategory(ev.EventId);
        ctx.AddEnrolment(participant.ParticipantId, category.CategoryId);
        var controller = new EnrolmentsController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var ok = Assert.IsType<OkObjectResult>(await controller.GetForEvent(ev.EventId));

        var list = Assert.IsAssignableFrom<IEnumerable<EnrolmentResponse>>(ok.Value).ToList();
        Assert.Single(list);
    }

    [Fact]
    public async Task GetForEvent_ByDifferentOrganiser_ReturnsForbidden()
    {
        using var ctx = TestHelpers.NewContext();
        var owner = ctx.AddOrganiser("johan@test.com");
        var other = ctx.AddOrganiser("lindiwe@test.com", "Lindiwe Khumalo");
        var ev = ctx.AddEvent(owner.OrganiserId);
        var controller = new EnrolmentsController(ctx).WithSession(other.OrganiserId, Roles.Organiser);

        var result = await controller.GetForEvent(ev.EventId);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task GetForEvent_MissingEvent_ReturnsNotFound()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var controller = new EnrolmentsController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var result = await controller.GetForEvent(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}