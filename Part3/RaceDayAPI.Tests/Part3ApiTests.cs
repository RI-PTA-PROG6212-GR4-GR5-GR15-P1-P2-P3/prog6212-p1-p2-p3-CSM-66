using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.DTOs;
using RaceDayAPI.Helpers;
using Xunit;

namespace RaceDayAPI.Tests;

public class Part3ApiTests
{
    // ---------- event filtering ----------

    [Fact]
    public async Task GetAll_FilterByType_ReturnsOnlyThatType()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        ctx.AddEvent(organiser.OrganiserId, name: "Park Run");
        var cycle = ctx.AddEvent(organiser.OrganiserId, name: "Cycle Tour");
        cycle.EventType = "Cycle";
        ctx.SaveChanges();
        var controller = new EventsController(ctx).WithSession();

        var ok = Assert.IsType<OkObjectResult>(await controller.GetAll(type: "cycle"));

        var list = Assert.IsAssignableFrom<IEnumerable<EventResponse>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal("Cycle Tour", list[0].Name);
    }

    [Fact]
    public async Task GetAll_Search_MatchesTheEventName()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        ctx.AddEvent(organiser.OrganiserId, name: "Pretoria Park Run");
        ctx.AddEvent(organiser.OrganiserId, name: "Soweto Marathon");
        var controller = new EventsController(ctx).WithSession();

        var ok = Assert.IsType<OkObjectResult>(await controller.GetAll(search: "Marathon"));

        var list = Assert.IsAssignableFrom<IEnumerable<EventResponse>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal("Soweto Marathon", list[0].Name);
    }

    [Fact]
    public async Task GetAll_FilterByLocation_ReturnsOnlyThatPlace()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        ctx.AddEvent(organiser.OrganiserId, name: "Pretoria Event");
        var polokwane = ctx.AddEvent(organiser.OrganiserId, name: "Polokwane Event");
        polokwane.Location = "Polokwane, Limpopo";
        ctx.SaveChanges();
        var controller = new EventsController(ctx).WithSession();

        var ok = Assert.IsType<OkObjectResult>(await controller.GetAll(location: "Polokwane"));

        var list = Assert.IsAssignableFrom<IEnumerable<EventResponse>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal("Polokwane Event", list[0].Name);
    }

    [Fact]
    public async Task GetAll_InvalidType_ReturnsBadRequest()
    {
        using var ctx = TestHelpers.NewContext();
        var controller = new EventsController(ctx).WithSession();

        var result = await controller.GetAll(type: "swim");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ---------- organiser dashboard ----------

    [Fact]
    public async Task GetMine_Events_ReturnsOnlyMyEventsWithEnrolmentTotals()
    {
        using var ctx = TestHelpers.NewContext();
        var johan = ctx.AddOrganiser("johan@test.com");
        var lindiwe = ctx.AddOrganiser("lindiwe@test.com", "Lindiwe Khumalo");
        var participant = ctx.AddParticipant();
        var upcoming = ctx.AddEvent(johan.OrganiserId, name: "Johan Upcoming");
        ctx.AddEvent(johan.OrganiserId, DateTime.Today.AddDays(-10), "Johan Past");
        ctx.AddEvent(lindiwe.OrganiserId, name: "Lindiwe Event");
        var category = ctx.AddCategory(upcoming.EventId);
        ctx.AddEnrolment(participant.ParticipantId, category.CategoryId);
        var controller = new EventsController(ctx).WithSession(johan.OrganiserId, Roles.Organiser);

        var ok = Assert.IsType<OkObjectResult>(await controller.GetMine());

        var list = Assert.IsAssignableFrom<IEnumerable<EventResponse>>(ok.Value).ToList();
        Assert.Equal(2, list.Count); // past and upcoming, but only Johan's
        Assert.DoesNotContain(list, e => e.Name == "Lindiwe Event");
        Assert.Equal((int?)1, list.Single(e => e.Name == "Johan Upcoming").TotalEnrolments);
        Assert.Equal((int?)0, list.Single(e => e.Name == "Johan Past").TotalEnrolments);
    }

    // ---------- statuses and results ----------

    [Fact]
    public async Task Capture_MarksTheEnrolmentAsCompleted()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var participant = ctx.AddParticipant();
        var ev = ctx.AddEvent(organiser.OrganiserId);
        var category = ctx.AddCategory(ev.EventId);
        var enrolment = ctx.AddEnrolment(participant.ParticipantId, category.CategoryId);
        Assert.Equal("Confirmed", enrolment.Status);
        var controller = new ResultsController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        await controller.Capture(new ResultRequest
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTime = TimeSpan.FromMinutes(50),
            Position = 1
        });

        Assert.Equal("Completed", ctx.Enrolments.Single().Status);
    }

    [Fact]
    public async Task Results_GetMine_IncludesFinisherTotalAndEventDate()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var sipho = ctx.AddParticipant("sipho@test.com");
        var anrich = ctx.AddParticipant("anrich@test.com", "Anrich Botha");
        var thabo = ctx.AddParticipant("thabo@test.com", "Thabo Mokoena");
        var ev = ctx.AddEvent(organiser.OrganiserId);
        var category = ctx.AddCategory(ev.EventId);

        var siphoEnrolment = ctx.AddEnrolment(sipho.ParticipantId, category.CategoryId);
        var anrichEnrolment = ctx.AddEnrolment(anrich.ParticipantId, category.CategoryId);
        ctx.AddEnrolment(thabo.ParticipantId, category.CategoryId); // no result for Thabo
        ctx.AddResult(siphoEnrolment.EnrolmentId, organiser.OrganiserId);
        ctx.AddResult(anrichEnrolment.EnrolmentId, organiser.OrganiserId);
        var controller = new ResultsController(ctx).WithSession(sipho.ParticipantId, Roles.Participant);

        var ok = Assert.IsType<OkObjectResult>(await controller.GetMine());

        var list = Assert.IsAssignableFrom<IEnumerable<ResultResponse>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal(2, list[0].TotalFinishers); // only people with a result count
        Assert.Equal(ev.EventDate, list[0].EventDate);
    }

    [Fact]
    public async Task Enrolments_GetForEvent_ShowsWhetherAResultWasCaptured()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var sipho = ctx.AddParticipant("sipho@test.com");
        var anrich = ctx.AddParticipant("anrich@test.com", "Anrich Botha");
        var ev = ctx.AddEvent(organiser.OrganiserId);
        var category = ctx.AddCategory(ev.EventId);
        var siphoEnrolment = ctx.AddEnrolment(sipho.ParticipantId, category.CategoryId);
        ctx.AddEnrolment(anrich.ParticipantId, category.CategoryId);
        ctx.AddResult(siphoEnrolment.EnrolmentId, organiser.OrganiserId);
        var controller = new EnrolmentsController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var ok = Assert.IsType<OkObjectResult>(await controller.GetForEvent(ev.EventId));

        var list = Assert.IsAssignableFrom<IEnumerable<EnrolmentResponse>>(ok.Value).ToList();
        Assert.Equal(2, list.Count);
        Assert.True(list.Single(e => e.ParticipantName == "Sipho Ndlovu").ResultCaptured);
        Assert.False(list.Single(e => e.ParticipantName == "Anrich Botha").ResultCaptured);
    }
}