using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public class DeleteSaleHandler : IRequestHandler<DeleteSaleCommand>
{
    private readonly ISaleRepository _repository;
    private readonly ILogger<DeleteSaleHandler> _logger;

    public DeleteSaleHandler(
        ISaleRepository repository,
        ILogger<DeleteSaleHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(
        DeleteSaleCommand request,
        CancellationToken cancellationToken)
    {
        var sale = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new SaleNotFoundException(request.Id);

        _repository.Remove(sale);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sale {SaleId} deleted.", sale.Id);
    }
}