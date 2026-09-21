using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public record GetSaleQuery(Guid Id) : IRequest<SaleResult>;
