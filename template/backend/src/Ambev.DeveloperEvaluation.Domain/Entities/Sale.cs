using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Exceptions;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class Sale : BaseEntity
{
    public string SaleNumber { get; private set; } = string.Empty;
    public DateTime SaleDate { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public Guid BranchId { get; private set; }
    public string BranchName { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public bool IsCancelled { get; private set; }

    public List<SaleItem> Items { get; private set; } = [];

    private Sale()
    {
    }

    public Sale(
        string saleNumber,
        DateTime saleDate,
        Guid customerId,
        string customerName,
        Guid branchId,
        string branchName,
        IEnumerable<SaleItem> items)
    {
        Id = Guid.NewGuid();

        Update(
            saleNumber,
            saleDate,
            customerId,
            customerName,
            branchId,
            branchName,
            items);
    }

    public void Update(
        string saleNumber,
        DateTime saleDate,
        Guid customerId,
        string customerName,
        Guid branchId,
        string branchName,
        IEnumerable<SaleItem> items)
    {
        if (IsCancelled)
            throw new SaleConflictException("A cancelled sale cannot be updated.");

        if (string.IsNullOrWhiteSpace(saleNumber) || saleNumber.Trim().Length > 50)
            throw new DomainException("Sale number must contain between 1 and 50 characters.");

        if (saleDate == default || saleDate.Kind != DateTimeKind.Utc)
            throw new DomainException("Sale date must be provided in UTC.");

        if (customerId == Guid.Empty || branchId == Guid.Empty)
            throw new DomainException("Customer ID and branch ID are required.");

        if (string.IsNullOrWhiteSpace(customerName) || customerName.Trim().Length > 200)
            throw new DomainException("Customer name must contain between 1 and 200 characters.");

        if (string.IsNullOrWhiteSpace(branchName) || branchName.Trim().Length > 200)
            throw new DomainException("Branch name must contain between 1 and 200 characters.");

        var newItems = items.ToList();

        if (newItems.Count == 0)
            throw new DomainException("A sale must contain at least one item.");

        if (newItems.Select(item => item.ProductId).Distinct().Count() != newItems.Count)
            throw new DomainException("Combine repeated products into a single item.");

        SaleNumber = saleNumber.Trim();
        SaleDate = saleDate;
        CustomerId = customerId;
        CustomerName = customerName.Trim();
        BranchId = branchId;
        BranchName = branchName.Trim();

        Items.Clear();
        Items.AddRange(newItems);
        TotalAmount = Items.Sum(item => item.TotalAmount);
    }

    public void Cancel()
    {
        IsCancelled = true;
    }
}