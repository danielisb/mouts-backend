using System.Linq.Expressions;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    public Task<Sale?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _context.Sales
            .Include(sale => sale.Items)
            .SingleOrDefaultAsync(sale => sale.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByNumberAsync(
        string number,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        return _context.Sales.AnyAsync(
            sale => sale.SaleNumber == number
                && (!exceptId.HasValue || sale.Id != exceptId.Value),
            cancellationToken);
    }

    public async Task<(IReadOnlyList<Sale> Sales, int TotalItems)> ListAsync(
        SaleFilter filter,
        CancellationToken cancellationToken)
    {
        var query = _context.Sales.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SaleNumber))
        {
            var pattern = ToLikePattern(filter.SaleNumber);
            query = query.Where(sale =>
                EF.Functions.Like(sale.SaleNumber, pattern, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(filter.CustomerName))
        {
            var pattern = ToLikePattern(filter.CustomerName);
            query = query.Where(sale =>
                EF.Functions.Like(sale.CustomerName, pattern, "\\"));
        }

        if (filter.CustomerId.HasValue)
            query = query.Where(sale => sale.CustomerId == filter.CustomerId.Value);

        if (filter.BranchId.HasValue)
            query = query.Where(sale => sale.BranchId == filter.BranchId.Value);

        if (filter.IsCancelled.HasValue)
            query = query.Where(sale => sale.IsCancelled == filter.IsCancelled.Value);

        if (filter.MinTotalAmount.HasValue)
            query = query.Where(sale => sale.TotalAmount >= filter.MinTotalAmount.Value);

        if (filter.MaxTotalAmount.HasValue)
            query = query.Where(sale => sale.TotalAmount <= filter.MaxTotalAmount.Value);

        if (filter.MinSaleDate.HasValue)
            query = query.Where(sale => sale.SaleDate >= filter.MinSaleDate.Value);

        if (filter.MaxSaleDate.HasValue)
            query = query.Where(sale => sale.SaleDate <= filter.MaxSaleDate.Value);

        var totalItems = await query.CountAsync(cancellationToken);
        var ordered = ApplyOrdering(query, filter.Order);

        var sales = await ordered
            .Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size)
            .Include(sale => sale.Items)
            .ToListAsync(cancellationToken);

        return (sales, totalItems);
    }

    public async Task AddAsync(Sale sale, CancellationToken cancellationToken)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
    }

    public void Remove(Sale sale)
    {
        _context.Sales.Remove(sale);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_Sales_SaleNumber"
            })
        {
            throw new SaleConflictException("Sale number already exists.");
        }
    }

    private static string ToLikePattern(string value)
    {
        return value.Trim()
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_")
            .Replace("*", "%");
    }

    private static IOrderedQueryable<Sale> ApplyOrdering(
        IQueryable<Sale> query,
        string order)
    {
        IOrderedQueryable<Sale>? ordered = null;

        foreach (var part in order.Split(','))
        {
            var tokens = part.Trim().Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length is < 1 or > 2)
                throw new DomainException("Invalid ordering.");

            var descending = tokens.Length == 2
                && tokens[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

            if (tokens.Length == 2
                && !descending
                && !tokens[1].Equals("asc", StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException("Order direction must be asc or desc.");
            }

            ordered = tokens[0] switch
            {
                "saleNumber" => AddOrder(query, ordered, sale => sale.SaleNumber, descending),
                "saleDate" => AddOrder(query, ordered, sale => sale.SaleDate, descending),
                "customerName" => AddOrder(query, ordered, sale => sale.CustomerName, descending),
                "totalAmount" => AddOrder(query, ordered, sale => sale.TotalAmount, descending),
                _ => throw new DomainException("Unsupported ordering field.")
            };
        }

        return ordered!.ThenBy(sale => sale.Id);
    }

    private static IOrderedQueryable<Sale> AddOrder<TKey>(
        IQueryable<Sale> query,
        IOrderedQueryable<Sale>? ordered,
        Expression<Func<Sale, TKey>> expression,
        bool descending)
    {
        if (ordered is null)
            return descending
                ? query.OrderByDescending(expression)
                : query.OrderBy(expression);

        return descending
            ? ordered.ThenByDescending(expression)
            : ordered.ThenBy(expression);
    }
}