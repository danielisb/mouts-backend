using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

public class ListSalesRequest
{
    [FromQuery(Name = "_page")]
    public int Page { get; set; } = 1;

    [FromQuery(Name = "_size")]
    public int Size { get; set; } = 10;

    [FromQuery(Name = "_order")]
    public string Order { get; set; } = "saleDate desc";

    public string? SaleNumber { get; set; }
    public string? CustomerName { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? BranchId { get; set; }
    public bool? IsCancelled { get; set; }

    [FromQuery(Name = "_minTotalAmount")]
    public decimal? MinTotalAmount { get; set; }

    [FromQuery(Name = "_maxTotalAmount")]
    public decimal? MaxTotalAmount { get; set; }

    [FromQuery(Name = "_minSaleDate")]
    public DateTime? MinSaleDate { get; set; }

    [FromQuery(Name = "_maxSaleDate")]
    public DateTime? MaxSaleDate { get; set; }

    public SaleFilter ToFilter()
    {
        return new SaleFilter
        {
            Page = Page,
            Size = Size,
            Order = Order,
            SaleNumber = SaleNumber,
            CustomerName = CustomerName,
            CustomerId = CustomerId,
            BranchId = BranchId,
            IsCancelled = IsCancelled,
            MinTotalAmount = MinTotalAmount,
            MaxTotalAmount = MaxTotalAmount,
            MinSaleDate = MinSaleDate?.ToUniversalTime(),
            MaxSaleDate = MaxSaleDate?.ToUniversalTime()
        };
    }
}