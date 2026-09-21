using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public class SaleInput
{
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public List<SaleItemInput> Items { get; set; } = [];

    public List<SaleItem> ToItems()
    {
        return Items.Select(item => new SaleItem(
            item.ProductId,
            item.ProductName,
            item.Quantity,
            item.UnitPrice)).ToList();
    }
}
