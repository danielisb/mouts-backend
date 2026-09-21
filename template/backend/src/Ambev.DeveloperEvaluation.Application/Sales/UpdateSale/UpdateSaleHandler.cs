using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateSaleHandler> _logger;

    public UpdateSaleHandler(
        ISaleRepository repository,
        IMapper mapper,
        ILogger<UpdateSaleHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SaleResult> Handle(
        UpdateSaleCommand request,
        CancellationToken cancellationToken)
    {
        await new SaleInputValidator()
            .ValidateAndThrowAsync(request.Input, cancellationToken);

        var sale = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new SaleNotFoundException(request.Id);

        var input = request.Input;

        if (await _repository.ExistsByNumberAsync(
            input.SaleNumber.Trim(), request.Id, cancellationToken))
        {
            throw new SaleConflictException("Sale number already exists.");
        }

        sale.Update(
            input.SaleNumber,
            input.SaleDate,
            input.CustomerId,
            input.CustomerName,
            input.BranchId,
            input.BranchName,
            input.ToItems());

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sale {SaleId} updated.", sale.Id);

        return _mapper.Map<SaleResult>(sale);
    }
}