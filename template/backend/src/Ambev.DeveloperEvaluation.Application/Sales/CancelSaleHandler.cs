using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public class CancelSaleHandler : IRequestHandler<CancelSaleCommand, SaleResult>
{
    private readonly ISaleRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<CancelSaleHandler> _logger;

    public CancelSaleHandler(
        ISaleRepository repository,
        IMapper mapper,
        ILogger<CancelSaleHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SaleResult> Handle(
        CancelSaleCommand request,
        CancellationToken cancellationToken)
    {
        var sale = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new SaleNotFoundException(request.Id);

        if (!sale.IsCancelled)
        {
            sale.Cancel();
            await _repository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Sale {SaleId} cancelled.", sale.Id);
        }

        return _mapper.Map<SaleResult>(sale);
    }
}