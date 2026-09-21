using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public record DeleteSaleCommand(Guid Id) : IRequest;
