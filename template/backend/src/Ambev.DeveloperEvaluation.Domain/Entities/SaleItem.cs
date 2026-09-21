using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class SaleItem : BaseEntity
{
    public Guid SaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Discount { get; private set; }
    public decimal TotalAmount { get; private set; }

    private SaleItem()
    {
    }

    public SaleItem(
        Guid productId,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        if (productId == Guid.Empty)
            throw new DomainException("Product ID is required.");

        if (string.IsNullOrWhiteSpace(productName) || productName.Trim().Length > 200)
            throw new DomainException("Product name must contain between 1 and 200 characters.");

        if (quantity < 1 || quantity > 20)
            throw new DomainException("Quantity must be between 1 and 20 per product.");

        if (unitPrice <= 0 || unitPrice > 1_000_000_000m)
            throw new DomainException("Unit price must be positive and at most 1000000000.");

        if (decimal.Round(unitPrice, 2) != unitPrice)
            throw new DomainException("Unit price must have at most two decimal places.");

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;

        var discountRate = quantity switch
        {
            >= 10 => 0.20m,
            >= 4 => 0.10m,
            _ => 0m
        };

        var grossAmount = quantity * unitPrice;

        Discount = decimal.Round(
            grossAmount * discountRate,
            2,
            MidpointRounding.AwayFromZero);

        TotalAmount = grossAmount - Discount;
    }
}