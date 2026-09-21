using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public record ListSalesQuery(SaleFilter Filter) : IRequest<SalePage>;
