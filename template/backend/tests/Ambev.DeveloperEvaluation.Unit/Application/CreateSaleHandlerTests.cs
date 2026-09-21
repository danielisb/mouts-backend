using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using Bogus;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class CreateSaleHandlerTests
{
    private readonly ISaleRepository _repository =
        Substitute.For<ISaleRepository>();

    private readonly IMapper _mapper;

    public CreateSaleHandlerTests()
    {
        var configuration = new MapperConfiguration(config =>
            config.AddProfile<SalesProfile>());

        configuration.AssertConfigurationIsValid();
        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public async Task Handle_ShouldSaveSaleWithCalculatedTotal()
    {
        var handler = CreateHandler();
        var input = CreateInput();

        var result = await handler.Handle(
            new CreateSaleCommand(input),
            CancellationToken.None);

        Assert.Equal(360m, result.TotalAmount);
        Assert.False(result.IsCancelled);
        Assert.Single(result.Items);

        await _repository.Received(1).AddAsync(
            Arg.Is<Sale>(sale => sale.TotalAmount == 360m),
            Arg.Any<CancellationToken>());

        await _repository.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRejectDuplicateNumber()
    {
        var input = CreateInput();

        _repository.ExistsByNumberAsync(
                input.SaleNumber,
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        await Assert.ThrowsAsync<SaleConflictException>(() =>
            CreateHandler().Handle(
                new CreateSaleCommand(input),
                CancellationToken.None));

        await _repository.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotSaveInvalidSale()
    {
        var input = CreateInput();
        input.Items[0].Quantity = 21;

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            CreateHandler().Handle(
                new CreateSaleCommand(input),
                CancellationToken.None));

        await _repository.DidNotReceive().AddAsync(
            Arg.Any<Sale>(),
            Arg.Any<CancellationToken>());
    }

    private CreateSaleHandler CreateHandler()
    {
        return new CreateSaleHandler(
            _repository,
            _mapper,
            NullLogger<CreateSaleHandler>.Instance);
    }

    private static SaleInput CreateInput()
    {
        var faker = new Faker("pt_BR")
        {
            Random = new Randomizer(123)
        };

        return new SaleInput
        {
            SaleNumber = "SALE-001",
            SaleDate = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc),
            CustomerId = Guid.NewGuid(),
            CustomerName = faker.Name.FullName(),
            BranchId = Guid.NewGuid(),
            BranchName = "Branch",
            Items =
            [
                new SaleItemInput
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Product",
                    Quantity = 4,
                    UnitPrice = 100m
                }
            ]
        };
    }
}