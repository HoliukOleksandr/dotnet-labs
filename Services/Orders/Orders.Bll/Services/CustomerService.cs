using AutoMapper;
using Orders.Bll.Dtos;
using Orders.Dal.Abstractions;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;

namespace Orders.Bll.Services;

public sealed class CustomerService(IUnitOfWork uow, IMapper mapper) : ICustomerService
{
    public async Task<CustomerDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var customer = await uow.Customers.GetByIdAsync(id, ct)
                       ?? throw new NotFoundException($"Customer {id} not found");
        return mapper.Map<CustomerDto>(customer);
    }

    public async Task<PagedResult<CustomerDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        ValidatePaging(page, pageSize);
        var items = await uow.Customers.GetPagedAsync(page, pageSize, ct);
        var total = await uow.Customers.CountAsync(ct);
        return new PagedResult<CustomerDto>(mapper.Map<List<CustomerDto>>(items), page, pageSize, total);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto input, CancellationToken ct = default)
    {
        // бізнес-правило, якому потрібен стан БД: email має бути унікальним
        if (await uow.Customers.GetByEmailAsync(input.Email, ct) is not null)
            throw new BusinessConflictException($"Customer with email '{input.Email}' already exists");

        var id = await uow.Customers.AddAsync(mapper.Map<Customer>(input), ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<CustomerDto> UpdateAsync(long id, UpdateCustomerDto input, CancellationToken ct = default)
    {
        var existing = await uow.Customers.GetByEmailAsync(input.Email, ct);
        if (existing is not null && existing.Id != id)
            throw new BusinessConflictException($"Customer with email '{input.Email}' already exists");

        var customer = mapper.Map<Customer>(input);
        customer.Id = id;
        if (!await uow.Customers.UpdateAsync(customer, ct))
            throw new NotFoundException($"Customer {id} not found");
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        if (!await uow.Customers.DeleteAsync(id, ct))
            throw new NotFoundException($"Customer {id} not found");
    }

    internal static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1) throw new ValidationException("Page must be >= 1");
        if (pageSize is < 1 or > 100) throw new ValidationException("PageSize must be between 1 and 100");
    }
}
