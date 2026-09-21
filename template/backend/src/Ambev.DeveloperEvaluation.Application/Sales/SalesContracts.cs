using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

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

public class SaleItemInput
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class SaleResult
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsCancelled { get; set; }
    public List<SaleItemResult> Items { get; set; } = [];
}

public class SaleItemResult
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
}

public record SalePage(
    IReadOnlyList<SaleResult> Data,
    int TotalItems,
    int CurrentPage,
    int TotalPages);

public record CreateSaleCommand(SaleInput Input) : IRequest<SaleResult>;
public record UpdateSaleCommand(Guid Id, SaleInput Input) : IRequest<SaleResult>;
public record GetSaleQuery(Guid Id) : IRequest<SaleResult>;
public record ListSalesQuery(SaleFilter Filter) : IRequest<SalePage>;
public record CancelSaleCommand(Guid Id) : IRequest<SaleResult>;
public record DeleteSaleCommand(Guid Id) : IRequest;