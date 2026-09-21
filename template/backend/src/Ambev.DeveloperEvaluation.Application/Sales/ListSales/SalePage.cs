namespace Ambev.DeveloperEvaluation.Application.Sales;

public record SalePage(
    IReadOnlyList<SaleResult> Data,
    int TotalItems,
    int CurrentPage,
    int TotalPages);
