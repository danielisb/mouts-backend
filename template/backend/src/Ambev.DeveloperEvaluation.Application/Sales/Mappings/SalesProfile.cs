using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public class SalesProfile : Profile
{
    public SalesProfile()
    {
        CreateMap<Sale, SaleResult>().MaxDepth(32);
        CreateMap<SaleItem, SaleItemResult>().MaxDepth(32);
    }
}
