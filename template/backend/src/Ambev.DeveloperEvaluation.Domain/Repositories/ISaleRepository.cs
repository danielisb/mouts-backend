using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNumberAsync(
        string number,
        Guid? exceptId,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<Sale> Sales, int TotalItems)> ListAsync(
        SaleFilter filter,
        CancellationToken cancellationToken);

    Task AddAsync(Sale sale, CancellationToken cancellationToken);

    void Remove(Sale sale);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}