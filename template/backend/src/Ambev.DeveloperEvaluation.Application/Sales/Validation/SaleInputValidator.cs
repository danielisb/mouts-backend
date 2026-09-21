using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales;

public class SaleInputValidator : AbstractValidator<SaleInput>
{
    public SaleInputValidator()
    {
        RuleFor(input => input.SaleNumber).NotEmpty().MaximumLength(50);
        RuleFor(input => input.CustomerId).NotEmpty();
        RuleFor(input => input.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(input => input.BranchId).NotEmpty();
        RuleFor(input => input.BranchName).NotEmpty().MaximumLength(200);

        RuleFor(input => input.SaleDate)
            .NotEmpty()
            .Must(date => date.Kind == DateTimeKind.Utc)
            .WithMessage("Sale date must be UTC, using the Z suffix.");

        RuleFor(input => input.Items).NotEmpty();

        RuleForEach(input => input.Items)
            .NotNull()
            .SetValidator(new SaleItemInputValidator());
    }
}

public class SaleItemInputValidator : AbstractValidator<SaleItemInput>
{
    public SaleItemInputValidator()
    {
        RuleFor(item => item.ProductId).NotEmpty();
        RuleFor(item => item.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(item => item.Quantity).InclusiveBetween(1, 20);

        RuleFor(item => item.UnitPrice)
            .GreaterThan(0)
            .LessThanOrEqualTo(1_000_000_000m);
    }
}