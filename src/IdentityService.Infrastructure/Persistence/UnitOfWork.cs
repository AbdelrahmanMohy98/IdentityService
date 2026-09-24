using IdentityService.Application.Common.Interfaces;

namespace IdentityService.Infrastructure.Persistence;

// Separate from ApplicationDbContext at the abstraction level (IUnitOfWork vs
// IApplicationDbContext) even though one class implements both here — keeps
// Application depending on two narrow interfaces instead of the concrete
// DbContext, so the persistence technology stays swappable in principle.
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
