using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ModernCleanCode.Application.Abstractions;
using ModernCleanCode.Application.Errors;

namespace ModernCleanCode.Infrastructure.Persistence;

public sealed class EfUnitOfWork(AppDbContext db) : IUnitOfWork
{
    // 2627 = violazione di vincolo PRIMARY KEY/UNIQUE, 2601 = indice univoco.
    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException
            { Number: UniqueConstraintViolation or UniqueIndexViolation })
        {
            throw new ConflictException("An entity with the same key already exists.");
        }
    }
}
