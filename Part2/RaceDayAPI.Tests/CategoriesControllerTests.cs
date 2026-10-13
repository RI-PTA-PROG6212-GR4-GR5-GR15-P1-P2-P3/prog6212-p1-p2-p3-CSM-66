using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.DTOs;
using RaceDayAPI.Helpers;
using Xunit;

namespace RaceDayAPI.Tests;

public class CategoriesControllerTests
{
    private static CategoryRequest NewCategory() => new() { Name = "10km Challenge", Distance = 10, Price = 120 };

    [Fact]
    public async Task Create_ByOwningOrganiser_ReturnsCreated()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var ev = ctx.AddEvent(organiser.OrganiserId);
        var controller = new CategoriesController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var result = await controller.Create(ev.EventId, NewCategory());

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        var body = Assert.IsType<CategoryResponse>(created.Value);
        Assert.Equal(ev.EventId, body.EventId);
        Assert.Equal(1, ctx.Categories.Count());
    }

    [Fact]
    public async Task Create_ByDifferentOrganiser_ReturnsForbidden()
    {
        using var ctx = TestHelpers.NewContext();
        var owner = ctx.AddOrganiser("johan@test.com");
        var other = ctx.AddOrganiser("lindiwe@test.com", "Lindiwe Khumalo");
        var ev = ctx.AddEvent(owner.OrganiserId);
        var controller = new CategoriesController(ctx).WithSession(other.OrganiserId, Roles.Organiser);

        var result = await controller.Create(ev.EventId, NewCategory());

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(0, ctx.Categories.Count());
    }

    [Fact]
    public async Task Create_ForMissingEvent_ReturnsNotFound()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var controller = new CategoriesController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var result = await controller.Create(999, NewCategory());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetForEvent_ReturnsOnlyThatEventsCategories()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var event1 = ctx.AddEvent(organiser.OrganiserId, name: "Event One");
        var event2 = ctx.AddEvent(organiser.OrganiserId, name: "Event Two");
        ctx.AddCategory(event1.EventId, "5km");
        ctx.AddCategory(event2.EventId, "21km");
        var controller = new CategoriesController(ctx).WithSession();

        var ok = Assert.IsType<OkObjectResult>(await controller.GetForEvent(event1.EventId));

        var list = Assert.IsAssignableFrom<IEnumerable<CategoryResponse>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal("5km", list[0].Name);
    }

    [Fact]
    public async Task Delete_CategoryWithEnrolments_ReturnsConflict()
    {
        using var ctx = TestHelpers.NewContext();
        var organiser = ctx.AddOrganiser();
        var participant = ctx.AddParticipant();
        var ev = ctx.AddEvent(organiser.OrganiserId);
        var category = ctx.AddCategory(ev.EventId);
        ctx.AddEnrolment(participant.ParticipantId, category.CategoryId);
        var controller = new CategoriesController(ctx).WithSession(organiser.OrganiserId, Roles.Organiser);

        var result = await controller.Delete(category.CategoryId);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(1, ctx.Categories.Count());
    }
}