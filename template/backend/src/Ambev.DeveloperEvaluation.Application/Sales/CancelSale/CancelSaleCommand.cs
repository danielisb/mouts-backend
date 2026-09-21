using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public record CancelSaleCommand(Guid Id) : IRequest<SaleResult>;
