using FishFarm.Domain.Entities;
using FishFarm.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FishFarm.Infrastructure.Persistence.Repositories;

public sealed class PersonRepository : BaseRepository<Person>, IPersonRepository
{
    public PersonRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<(Person Person, int FarmCount)> Items, int TotalCount)> GetPagedAsync(
        int      pageNumber,
        int      pageSize,
        string?  search      = null,
        bool?    certExpired = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = DbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Email.Contains(search));

        if (certExpired.HasValue)
            query = certExpired.Value
                ? query.Where(p => p.CertifiedUntil < today)
                : query.Where(p => p.CertifiedUntil >= today);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new { Person = p, FarmCount = p.FarmWorkers.Count() })
            .ToListAsync(cancellationToken);

        return (items.Select(x => (x.Person, x.FarmCount)).ToList(), total);
    }

    public async Task<Person?> GetByIdWithAssignmentsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(p => p.FarmWorkers)
                .ThenInclude(fw => fw.FishFarm)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<bool> EmailExistsAsync(
        string email,
        Guid?  excludePersonId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(p => p.Email == email);
        if (excludePersonId.HasValue)
            query = query.Where(p => p.Id != excludePersonId.Value);
        return await query.AnyAsync(cancellationToken);
    }
}
