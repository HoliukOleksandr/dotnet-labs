using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Mapping;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductDto>();
        CreateMap<ProductDetail, ProductDetailDto>();
        CreateMap<ProductImage, ProductImageDto>();
        CreateMap<ProductCategory, ProductCategoryDto>();
        CreateMap<Product, ProductDetailsDto>()
            .ForMember(d => d.Categories, o => o.MapFrom(s => s.ProductCategories));
    }
}

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDto>();
        CreateMap<ProductCategory, CategoryProductDto>();
        CreateMap<Category, CategoryWithProductsDto>()
            .ForMember(d => d.Products, o => o.MapFrom(s => s.ProductCategories));
    }
}
