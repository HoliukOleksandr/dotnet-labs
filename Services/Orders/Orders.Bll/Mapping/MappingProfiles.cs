using AutoMapper;
using Orders.Bll.Dtos;
using Orders.Domain.Models;

namespace Orders.Bll.Mapping;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<OrderItem, OrderItemDto>();
        CreateMap<Order, OrderDto>();
        CreateMap<OrderSummary, OrderSummaryDto>();
    }
}

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>();
        CreateMap<CreateCustomerDto, Customer>().ForMember(d => d.Id, o => o.Ignore());
        CreateMap<UpdateCustomerDto, Customer>().ForMember(d => d.Id, o => o.Ignore());
    }
}

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductDto>();
    }
}
