using IdentityService.Application.Common.Interfaces;
using IdentityService.Domain.Common;
using IdentityService.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext, IUnitOfWork
{
    private readonly IPublisher _publisher;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IPublisher publisher)
        : base(options)
    {
        _publisher = publisher;
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect events from every tracked aggregate *before* saving, then
        // dispatch them *after* the transaction commits successfully — so
        // handlers reacting to e.g. UserRegisteredDomainEvent only ever see
        // events for state that actually made it to the database, and a
        // failed commit never leaves "phantom" side effects fired for nothing.
        var aggregatesWithEvents = ChangeTracker.Entries<AggregateRoot<UserId>>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count != 0)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        var domainEvents = aggregatesWithEvents.SelectMany(e => e.DomainEvents).ToList();
        aggregatesWithEvents.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        return result;
    }
}
