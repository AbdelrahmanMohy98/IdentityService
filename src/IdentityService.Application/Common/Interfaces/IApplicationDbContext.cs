using IdentityService.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Application.Common.Interfaces;

// The Application layer depends only on this thin abstraction, never on
// EF Core's DbContext directly — Infrastructure provides the real implementation.
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
