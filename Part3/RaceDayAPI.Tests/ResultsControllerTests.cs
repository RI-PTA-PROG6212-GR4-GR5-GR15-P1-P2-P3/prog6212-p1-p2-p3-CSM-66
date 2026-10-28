using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Helpers;
using RaceDayAPI.Models;
using Xunit;

namespace RaceDayAPI.Tests;

public class ResultsControllerTests
{
    // organiser owns an event, participant is enrolled in it
    private static (Organiser organiser, Participant participant, Enrolment enrolment) Setup(RaceDayContext ctx)
    {
        var organiser = ctx.AddOrganiser();
        var participant = ctx.AddParticipant();
        var ev = ctx.AddEvent(organiser.OrganiserId);
        var category = ctx.AddCategory(ev.EventId);
        var enrolment = ctx.AddEnrolment(participant.ParticipantId, category.CategoryId);
        return (organiser, participant, enrolment);
    }

    private static ResultRequest NewResult(int enrolmentId) => new()
    {
        EnrolmentId = enrolmentId,
        FinishTime = TimeSpan.FromMinutes(52),
        Position = 4
    };

    [Fact]
    public async Task Capture_ByOwningOrganiser_ReturnsCreated()
    {
        using var ctx = TestHelpers.NewContext();
        var (organiser, _, enrolment) = Setup(ctx);
        var controller = new ResultsController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var result = await controller.Capture(NewResult(enrolment.EnrolmentId));

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        var saved = ctx.Results.Single();
        Assert.Equal(enrolment.EnrolmentId, saved.EnrolmentId);
        Assert.Equal(4, saved.Position);
        Assert.Equal(organiser.OrganiserId, saved.CapturedByOrganiserId);
    }

    [Fact]
    public async Task Capture_ByDifferentOrganiser_ReturnsForbidden()
    {
        using var ctx = TestHelpers.NewContext();
        var (_, _, enrolment) = Setup(ctx);
        var other = ctx.AddOrganiser("lindiwe@test.com", "Lindiwe Khumalo");
        var controller = new ResultsController(ctx).WithSession(other.OrganiserId, Roles.Organiser);

        var result = await controller.Capture(NewResult(enrolment.EnrolmentId));

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(0, ctx.Results.Count());
    }

    [Fact]
    public async Task Capture_SecondResultForSameEnrolment_ReturnsConflict()
    {
        using var ctx = TestHelpers.NewContext();
        var (organiser, _, enrolment) = Setup(ctx);
        ctx.AddResult(enrolment.EnrolmentId, organiser.OrganiserId);
        var controller = new ResultsController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var result = await controller.Capture(NewResult(enrolment.EnrolmentId));

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(1, ctx.Results.Count());
    }

    [Fact]
    public async Task Capture_UnknownEnrolment_ReturnsNotFound()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var controller = new ResultsController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var result = await controller.Capture(NewResult(999));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyTheLoggedInParticipantsResults()
    {
        using var ctx = TestHelpers.NewContext();
        var (organiser, participant, enrolment) = Setup(ctx);

        // a second participant with their own result that sipho must NOT see
        var anrich = ctx.AddParticipant("anrich@test.com", "Anrich Botha");
        var ev = ctx.AddEvent(organiser.OrganiserId, name: "Other Event");
        var category = ctx.AddCategory(ev.EventId);
        var anrichEnrolment = ctx.AddEnrolment(anrich.ParticipantId, category.CategoryId);
        ctx.AddResult(enrolment.EnrolmentId, organiser.OrganiserId);
        ctx.AddResult(anrichEnrolment.EnrolmentId, organiser.OrganiserId);

        var controller = new ResultsController(ctx).WithSession(participant.ParticipantId, Roles.Participant);

        var ok = Assert.IsType<OkObjectResult>(await controller.GetMine());

        var list = Assert.IsAssignableFrom<IEnumerable<ResultResponse>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal(enrolment.EnrolmentId, list[0].EnrolmentId);
    }
}