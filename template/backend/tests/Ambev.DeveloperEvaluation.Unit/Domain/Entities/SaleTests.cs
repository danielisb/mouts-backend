using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleTests
{
    [Theory]
    [InlineData(1, 0, 100)]
    [InlineData(3, 0, 300)]
    [InlineData(4, 40, 360)]
    [InlineData(9, 90, 810)]
    [InlineData(10, 200, 800)]
    [InlineData(20, 400, 1600)]
    public void Item_ShouldApplyDiscountByQuantity(
        int quantity,
        decimal expectedDiscount,
        decimal expectedTotal)
    {
        var item = new SaleItem(Guid.NewGuid(), "Product", quantity, 100m);

        Assert.Equal(expectedDiscount, item.Discount);
        Assert.Equal(expectedTotal, item.TotalAmount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(21)]
    public void Item_ShouldRejectInvalidQuantity(int quantity)
    {
        Assert.Throws<DomainException>(() =>
            new SaleItem(Guid.NewGuid(), "Product", quantity, 100m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Item_ShouldRejectInvalidPrice(decimal price)
    {
        Assert.Throws<DomainException>(() =>
            new SaleItem(Guid.NewGuid(), "Product", 1, price));
    }

    [Fact]
    public void Item_ShouldRejectPriceWithMoreThanTwoDecimalPlaces()
    {
        Assert.Throws<DomainException>(() =>
            new SaleItem(Guid.NewGuid(), "Product", 1, 10.001m));
    }

    [Fact]
    public void Item_ShouldRoundDiscountToTwoDecimalPlaces()
    {
        var item = new SaleItem(Guid.NewGuid(), "Product", 4, 0.04m);

        Assert.Equal(0.02m, item.Discount);
        Assert.Equal(0.14m, item.TotalAmount);
    }

    [Fact]
    public void Sale_ShouldCalculateDiscountForEachProductSeparately()
    {
        var sale = CreateSale(
            new SaleItem(Guid.NewGuid(), "Product A", 3, 100m),
            new SaleItem(Guid.NewGuid(), "Product B", 3, 100m));

        Assert.Equal(600m, sale.TotalAmount);
        Assert.All(sale.Items, item => Assert.Equal(0m, item.Discount));
    }

    [Fact]
    public void Sale_ShouldRejectRepeatedProduct()
    {
        var productId = Guid.NewGuid();

        Assert.Throws<DomainException>(() => CreateSale(
            new SaleItem(productId, "Product", 15, 100m),
            new SaleItem(productId, "Product", 15, 100m)));
    }

    [Fact]
    public void Sale_ShouldRejectEmptyItems()
    {
        Assert.Throws<DomainException>(() => CreateSale());
    }

    [Fact]
    public void Sale_ShouldRecalculateTotalOnUpdate()
    {
        var sale = CreateSale(
            new SaleItem(Guid.NewGuid(), "Product", 4, 100m));

        sale.Update(
            sale.SaleNumber,
            sale.SaleDate,
            sale.CustomerId,
            sale.CustomerName,
            sale.BranchId,
            sale.BranchName,
            [new SaleItem(Guid.NewGuid(), "Product", 10, 100m)]);

        Assert.Single(sale.Items);
        Assert.Equal(800m, sale.TotalAmount);
    }

    [Fact]
    public void Sale_ShouldPreserveAmountWhenCancelled()
    {
        var sale = CreateSale(
            new SaleItem(Guid.NewGuid(), "Product", 4, 100m));

        sale.Cancel();
        sale.Cancel();

        Assert.True(sale.IsCancelled);
        Assert.Equal(360m, sale.TotalAmount);
    }

    [Fact]
    public void Sale_ShouldRejectUpdateAfterCancellation()
    {
        var sale = CreateSale(
            new SaleItem(Guid.NewGuid(), "Product", 4, 100m));

        sale.Cancel();

        Assert.Throws<SaleConflictException>(() => sale.Update(
            sale.SaleNumber,
            sale.SaleDate,
            sale.CustomerId,
            sale.CustomerName,
            sale.BranchId,
            sale.BranchName,
            [new SaleItem(Guid.NewGuid(), "Product", 10, 100m)]));
    }

    private static Sale CreateSale(params SaleItem[] items)
    {
        return new Sale(
            "SALE-001",
            new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc),
            Guid.NewGuid(),
            "Customer",
            Guid.NewGuid(),
            "Branch",
            items);
    }
}