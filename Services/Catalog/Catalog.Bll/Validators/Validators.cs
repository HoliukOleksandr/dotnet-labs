using Catalog.Bll.Dtos;
using FluentValidation;

namespace Catalog.Bll.Validators;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        // Name перевіряється DataAnnotations; тут — правила, яких там немає
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50)
            .Matches("^[A-Z0-9-]+$").WithMessage("SKU may contain only capital letters, digits and '-'");
        RuleFor(x => x.Price).GreaterThan(0).PrecisionScale(18, 2, false);
    }
}

public class ChangePriceDtoValidator : AbstractValidator<ChangePriceDto>
{
    public ChangePriceDtoValidator()
    {
        RuleFor(x => x.Price).GreaterThan(0).PrecisionScale(18, 2, false);
    }
}

public class StockChangeDtoValidator : AbstractValidator<StockChangeDto>
{
    public StockChangeDtoValidator()
    {
        RuleFor(x => x.Quantity).InclusiveBetween(1, 100_000);
    }
}

public class ProductQueryValidator : AbstractValidator<ProductQuery>
{
    private static readonly string[] SortFields = { "name", "price", "stock" };
    private static readonly string[] SortDirections = { "asc", "desc" };

    public ProductQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
        RuleFor(x => x).Must(x => x.MinPrice is null || x.MaxPrice is null || x.MinPrice <= x.MaxPrice)
            .WithName("MinPrice").WithMessage("MinPrice must not be greater than MaxPrice");
        RuleFor(x => x.SortBy).Must(v => SortFields.Contains(v.ToLowerInvariant()))
            .WithMessage($"SortBy must be one of: {string.Join(", ", SortFields)}");
        RuleFor(x => x.SortDir).Must(v => SortDirections.Contains(v.ToLowerInvariant()))
            .WithMessage("SortDir must be 'asc' or 'desc'");
    }
}
