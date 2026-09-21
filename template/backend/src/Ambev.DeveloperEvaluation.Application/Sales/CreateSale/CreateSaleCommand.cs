using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public record CreateSaleCommand(SaleInput Input) : IRequest<SaleResult>;
