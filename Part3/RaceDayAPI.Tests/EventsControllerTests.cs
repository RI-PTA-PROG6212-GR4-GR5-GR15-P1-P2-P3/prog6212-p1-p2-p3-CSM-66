using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.DTOs;
using RaceDayAPI.Helpers;
using Xunit;

namespace RaceDayAPI.Tests;

public class EventsControllerTests
{
    private static EventRequest ValidRequest(string name = "Pretoria Park Run") => new()
    {
        Name = name,
        Description = "Community road run.",
        EventDate = DateTime.Today.AddDays(30),
        Location = "Pretoria, Gauteng",
        Distance = 10,
        EventType = "run"
    };

    // ---------- viewing (public) ----------

    [Fact]
    public async Task GetAll_ReturnsOnlyUpcomingEvents()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        ctx.AddEvent(organiser.OrganiserId, DateTime.Today.AddDays(-5), "Old Event");
        ctx.AddEvent(organiser.OrganiserId, DateTime.Today.AddDays(10), "Future Event");
        var controller = new EventsController(ctx).WithSession();

        var ok = Assert.IsType<OkObjectResult>(await controller.GetAll());

        var list = Assert.IsAssignableFrom<IEnumerable<EventResponse>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal("Future Event", list[0].Name);
    }

    [Fact]
    public async Task GetById_ExistingEvent_ReturnsEventWithCategories()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var ev = ctx.AddEvent(organiser.OrganiserId);
        ctx.AddCategory(ev.EventId, "10km");
        var controller = new EventsController(ctx).WithSession();

        var ok = Assert.IsType<OkObjectResult>(await controller.GetById(ev.EventId));

        var body = Assert.IsType<EventResponse>(ok.Value);
        Assert.Equal(ev.EventId, body.EventId);
        Assert.Single(body.Categories!);
    }

    [Fact]
    public async Task GetById_MissingEvent_ReturnsNotFound()
    {
        using var ctx = TestHelpers.NewContext();
        var controller = new EventsController(ctx).WithSession();

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- creating ----------

    [Fact]
    public async Task Create_AsOrganiser_ReturnsCreatedAndLinksOrganiser()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var controller = new EventsController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var result = await controller.Create(ValidRequest());

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var body = Assert.IsType<EventResponse>(created.Value);
        Assert.Equal("Run", body.EventType); // "run" gets tidied up to "Run"
        Assert.Equal(organiser.OrganiserId, body.OrganiserId);
        Assert.Equal(1, ctx.Events.Count());
    }

    [Fact]
    public async Task Create_InvalidEventType_ReturnsBadRequest()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var controller = new EventsController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);
        var request = ValidRequest();
        request.EventType = "swim";

        var result = await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, ctx.Events.Count());
    }

    // ---------- updating ----------

    [Fact]
    public async Task Update_OwnEvent_ReturnsOkAndSavesChanges()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var ev = ctx.AddEvent(organiser.OrganiserId);
        var controller = new EventsController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var result = await controller.Update(ev.EventId, ValidRequest("Renamed Event"));

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Renamed Event", ctx.Events.Single().Name);
    }

    [Fact]
    public async Task Update_SomeoneElsesEvent_ReturnsForbidden()
    {
        using var ctx = TestHelpers.NewContext();
        var owner = ctx.AddOrganiser("johan@test.com");
        var other = ctx.AddOrganiser("lindiwe@test.com", "Lindiwe Khumalo");
        var ev = ctx.AddEvent(owner.OrganiserId, name: "Original Name");
        var controller = new EventsController(ctx).WithSession(other.OrganiserId, Roles.Organiser);

        var result = await controller.Update(ev.EventId, ValidRequest("Hijacked"));

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal("Original Name", ctx.Events.Single().Name);
    }

    [Fact]
    public async Task Update_MissingEvent_ReturnsNotFound()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var controller = new EventsController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var result = await controller.Update(999, ValidRequest());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- deleting ----------

    [Fact]
    public async Task Delete_OwnEventWithNoEnrolments_ReturnsNoContentAndRemovesIt()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var ev = ctx.AddEvent(organiser.OrganiserId);
        ctx.AddCategory(ev.EventId);
        var controller = new EventsController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var result = await controller.Delete(ev.EventId);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, ctx.Events.Count());
        Assert.Equal(0, ctx.Categories.Count());
    }

    [Fact]
    public async Task Delete_EventWithEnrolments_ReturnsConflict()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var participant = ctx.AddParticipant();
        var ev = ctx.AddEvent(organiser.OrganiserId);
        var category = ctx.AddCategory(ev.EventId);
        ctx.AddEnrolment(participant.ParticipantId, category.CategoryId);
        var controller = new EventsController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var result = await controller.Delete(ev.EventId);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(1, ctx.Events.Count());
    }

    [Fact]
    public async Task Delete_SomeoneElsesEvent_ReturnsForbidden()
    {
        using var ctx = TestHelpers.NewContext();
        var owner = ctx.AddOrganiser("johan@test.com");
        var other = ctx.AddOrganiser("lindiwe@test.com", "Lindiwe Khumalo");
        var ev = ctx.AddEvent(owner.OrganiserId);
        var controller = new EventsController(ctx).WithSession(other.OrganiserId, Roles.Organiser);

        var result = await controller.Delete(ev.EventId);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(1, ctx.Events.Count());
    }
}