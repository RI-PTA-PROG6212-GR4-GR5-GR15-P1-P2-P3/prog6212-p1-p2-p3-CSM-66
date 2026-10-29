using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Models;

namespace RaceDayAPI.Data;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options) { }

    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // emails must be unique within each table
        modelBuilder.Entity<Organiser>().HasIndex(o => o.Email).IsUnique();
        modelBuilder.Entity<Participant>().HasIndex(p => p.Email).IsUnique();

        // a participant can only enrol once per category
        modelBuilder.Entity<Enrolment>()
            .HasIndex(e => new { e.ParticipantId, e.CategoryId })
            .IsUnique();

        // one result per enrolment (one-to-one)
        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Result)
            .WithOne(r => r.Enrolment)
            .HasForeignKey<Result>(r => r.EnrolmentId);

        modelBuilder.Entity<Result>()
            .HasOne(r => r.CapturedByOrganiser)
            .WithMany(o => o.CapturedResults)
            .HasForeignKey(r => r.CapturedByOrganiserId);

        // defaults
        modelBuilder.Entity<Organiser>().Property(o => o.CreatedAt).HasDefaultValueSql("GETDATE()");
        modelBuilder.Entity<Participant>().Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");
        modelBuilder.Entity<Event>().Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
        modelBuilder.Entity<Event>().Property(e => e.EventType).HasDefaultValue("Run");
        modelBuilder.Entity<Enrolment>().Property(e => e.EnrolmentDate).HasDefaultValueSql("GETDATE()");
        modelBuilder.Entity<Enrolment>().Property(e => e.Status).HasDefaultValue("Confirmed");

        // no cascade deletes, avoids SQL Server's multiple cascade path error
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}