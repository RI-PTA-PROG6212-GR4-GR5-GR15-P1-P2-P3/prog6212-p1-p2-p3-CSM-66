using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Helpers;
using RaceDayAPI.Models;

namespace RaceDayAPI.Tests;

// fake session so controllers can use HttpContext.Session without a real server running
public class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
}

public static class TestHelpers
{
    // every test gets its own fresh in-memory database
    public static RaceDayContext NewContext()
    {
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new RaceDayContext(options);
    }

    // gives a controller a fake session. no userId/role = not logged in
    public static T WithSession<T>(this T controller, int? userId = null, string? role = null) where T : ControllerBase
    {
        var session = new FakeSession();
        if (userId != null) session.SetInt32(SessionKeys.UserId, userId.Value);
        if (role != null) session.SetString(SessionKeys.Role, role);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = session }
        };
        return controller;
    }

    public static Organiser AddOrganiser(this RaceDayContext ctx, string email = "johan@test.com", string name = "Johan Pretorius")
    {
        var organiser = new Organiser
        {
            Name = name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123", 4),
            CreatedAt = DateTime.Now
        };
        ctx.Organisers.Add(organiser);
        ctx.SaveChanges();
        return organiser;
    }

    public static Participant AddParticipant(this RaceDayContext ctx, string email = "sipho@test.com", string name = "Sipho Ndlovu")
    {
        var participant = new Participant
        {
            Name = name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123", 4),
            CreatedAt = DateTime.Now
        };
        ctx.Participants.Add(participant);
        ctx.SaveChanges();
        return participant;
    }

    public static Event AddEvent(this RaceDayContext ctx, int organiserId, DateTime? date = null, string name = "Test Run")
    {
        var ev = new Event
        {
            OrganiserId = organiserId,
            Name = name,
            Description = "Test event",
            EventDate = (date ?? DateTime.Today.AddDays(30)).Date,
            Location = "Pretoria, Gauteng",
            Distance = 10,
            EventType = "Run",
            CreatedAt = DateTime.Now
        };
        ctx.Events.Add(ev);
        ctx.SaveChanges();
        return ev;
    }

    public static Category AddCategory(this RaceDayContext ctx, int eventId, string name = "10km")
    {
        var category = new Category { EventId = eventId, Name = name, Distance = 10, Price = 100 };
        ctx.Categories.Add(category);
        ctx.SaveChanges();
        return category;
    }

    public static Enrolment AddEnrolment(this RaceDayContext ctx, int participantId, int categoryId)
    {
        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            CategoryId = categoryId,
            EnrolmentDate = DateTime.Now,
            Status = "Registered"
        };
        ctx.Enrolments.Add(enrolment);
        ctx.SaveChanges();
        return enrolment;
    }

    public static Result AddResult(this RaceDayContext ctx, int enrolmentId, int organiserId)
    {
        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = TimeSpan.FromMinutes(50),
            Position = 1,
            CapturedByOrganiserId = organiserId
        };
        ctx.Results.Add(result);
        ctx.SaveChanges();
        return result;
    }
}