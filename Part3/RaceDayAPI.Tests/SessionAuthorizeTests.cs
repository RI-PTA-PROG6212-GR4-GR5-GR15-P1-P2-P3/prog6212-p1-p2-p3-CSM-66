using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDayAPI.Controllers;
using RaceDayAPI.Filters;
using RaceDayAPI.Helpers;
using Xunit;

namespace RaceDayAPI.Tests;

public class SessionAuthorizeTests
{
    private static AuthorizationFilterContext BuildContext(int? userId = null, string? role = null)
    {
        var session = new FakeSession();
        if (userId != null) session.SetInt32(SessionKeys.UserId, userId.Value);
        if (role != null) session.SetString(SessionKeys.Role, role);

        var http = new DefaultHttpContext { Session = session };
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    // ---------- the filter itself ----------

    [Fact]
    public void NotLoggedIn_IsRejectedWith401()
    {
        var context = BuildContext();

        new SessionAuthorizeAttribute().OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Theory]
    [InlineData(Roles.Organiser)]
    [InlineData(Roles.Participant)]
    public void NotLoggedIn_OnRoleEndpoint_IsRejectedWith401(string requiredRole)
    {
        var context = BuildContext();

        new SessionAuthorizeAttribute(requiredRole).OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Theory]
    [InlineData(Roles.Organiser, Roles.Participant)]
    [InlineData(Roles.Participant, Roles.Organiser)]
    public void WrongRole_IsRejectedWith403(string requiredRole, string actualRole)
    {
        var context = BuildContext(1, actualRole);

        new SessionAuthorizeAttribute(requiredRole).OnAuthorization(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Organiser)]
    [InlineData(Roles.Participant)]
    public void CorrectRole_IsAllowed(string role)
    {
        var context = BuildContext(1, role);

        new SessionAuthorizeAttribute(role).OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData(Roles.Organiser)]
    [InlineData(Roles.Participant)]
    public void EndpointWithoutRole_AllowsAnyLoggedInUser(string role)
    {
        var context = BuildContext(1, role);

        new SessionAuthorizeAttribute().OnAuthorization(context);

        Assert.Null(context.Result);
    }

    // ---------- every protected endpoint really carries the right role ----------

    [Theory]
    [InlineData(typeof(EventsController), nameof(EventsController.Create), Roles.Organiser)]
    [InlineData(typeof(EventsController), nameof(EventsController.Update), Roles.Organiser)]
    [InlineData(typeof(EventsController), nameof(EventsController.Delete), Roles.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Create), Roles.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Update), Roles.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Delete), Roles.Organiser)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.Enrol), Roles.Participant)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetMine), Roles.Participant)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetForEvent), Roles.Organiser)]
    [InlineData(typeof(ResultsController), nameof(ResultsController.Capture), Roles.Organiser)]
    [InlineData(typeof(ResultsController), nameof(ResultsController.GetMine), Roles.Participant)]
    [InlineData(typeof(ProfileController), nameof(ProfileController.GetParticipant), Roles.Participant)]
    [InlineData(typeof(ProfileController), nameof(ProfileController.UpdateParticipant), Roles.Participant)]
    [InlineData(typeof(ProfileController), nameof(ProfileController.GetOrganiser), Roles.Organiser)]
    [InlineData(typeof(ProfileController), nameof(ProfileController.UpdateOrganiser), Roles.Organiser)]
    public void ProtectedEndpoint_EnforcesItsRole(Type controllerType, string methodName, string requiredRole)
    {
        var attribute = controllerType.GetMethod(methodName)!
            .GetCustomAttributes(typeof(SessionAuthorizeAttribute), true)
            .Cast<SessionAuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute); // endpoint must be protected at all

        var otherRole = requiredRole == Roles.Organiser ? Roles.Participant : Roles.Organiser;

        var anonymous = BuildContext();
        attribute!.OnAuthorization(anonymous);
        Assert.IsType<UnauthorizedObjectResult>(anonymous.Result);

        var wrongRole = BuildContext(1, otherRole);
        attribute.OnAuthorization(wrongRole);
        Assert.Equal(403, Assert.IsType<ObjectResult>(wrongRole.Result).StatusCode);

        var rightRole = BuildContext(1, requiredRole);
        attribute.OnAuthorization(rightRole);
        Assert.Null(rightRole.Result);
    }
}