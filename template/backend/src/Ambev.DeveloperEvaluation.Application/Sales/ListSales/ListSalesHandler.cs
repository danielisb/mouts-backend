using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public class ListSalesHandler : IRequestHandler<ListSalesQuery, SalePage>
{
    private readonly ISaleRepository _repository;
    private readonly IMapper _mapper;

    public ListSalesHandler(ISaleRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<SalePage> Handle(
        ListSalesQuery request,
        CancellationToken cancellationToken)
    {
        var filter = request.Filter;

        if (filter.Page < 1 || filter.Size < 1 || filter.Size > 100)
            throw new DomainException("Page must be positive and size must be between 1 and 100.");

        if ((long)(filter.Page - 1) * filter.Size > int.MaxValue)
            throw new DomainException("Requested page is too large.");

        if (string.IsNullOrWhiteSpace(filter.Order))
            throw new DomainException("Ordering must not be empty.");

        if (filter.MinTotalAmount > filter.MaxTotalAmount)
            throw new DomainException("Minimum total cannot exceed maximum total.");

        if (filter.MinSaleDate > filter.MaxSaleDate)
            throw new DomainException("Minimum date cannot exceed maximum date.");

        if (filter.MinSaleDate is DateTime min && min.Kind != DateTimeKind.Utc)
            throw new DomainException("Minimum date must be UTC.");

        if (filter.MaxSaleDate is DateTime max && max.Kind != DateTimeKind.Utc)
            throw new DomainException("Maximum date must be UTC.");

        var (sales, totalItems) = await _repository.ListAsync(
            filter, cancellationToken);

        return new SalePage(
            _mapper.Map<List<SaleResult>>(sales),
            totalItems,
            filter.Page,
            (int)Math.Ceiling(totalItems / (double)filter.Size));
    }
}