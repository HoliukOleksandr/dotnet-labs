using Orders.Bll.Dtos;

namespace Orders.Bll.Services;

public interface ICustomerService
{
    Task<CustomerDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PagedResult<CustomerDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerDto input, CancellationToken ct = default);
    Task<CustomerDto> UpdateAsync(long id, UpdateCustomerDto input, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
