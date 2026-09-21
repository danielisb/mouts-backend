using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, SaleResult>
{
    private readonly ISaleRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateSaleHandler> _logger;

    public CreateSaleHandler(
        ISaleRepository repository,
        IMapper mapper,
        ILogger<CreateSaleHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SaleResult> Handle(
        CreateSaleCommand request,
        CancellationToken cancellationToken)
    {
        var input = request.Input;

        await new SaleInputValidator()
            .ValidateAndThrowAsync(input, cancellationToken);

        if (await _repository.ExistsByNumberAsync(
            input.SaleNumber.Trim(), null, cancellationToken))
        {
            throw new SaleConflictException("Sale number already exists.");
        }

        var sale = new Sale(
            input.SaleNumber,
            input.SaleDate,
            input.CustomerId,
            input.CustomerName,
            input.BranchId,
            input.BranchName,
            input.ToItems());

        await _repository.AddAsync(sale, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Sale {SaleId} created.", sale.Id);

        return _mapper.Map<SaleResult>(sale);
    }
}