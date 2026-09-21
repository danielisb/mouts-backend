using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public record UpdateSaleCommand(Guid Id, SaleInput Input) : IRequest<SaleResult>;
