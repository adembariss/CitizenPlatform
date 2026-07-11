using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CitizenPlatform.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly CitizenPlatformDbContext _dbContext;

    public EfUnitOfWork(CitizenPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var result = await operation(cancellationToken);
                if (result is IResult { IsSuccess: false })
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return result;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
