using System.Data;
using Microsoft.Data.SqlClient;
using Orders.Dal.Abstractions;
using Orders.Domain.Models;

namespace Orders.Dal.Repositories;

/// <summary>Репозиторій на чистому ADO.NET: ручні SqlCommand, лише параметри, await using для звільнення ресурсів.</summary>
public sealed class CustomerRepository : RepositoryBase, ICustomerRepository
{
    public CustomerRepository(SqlConnection c, Func<SqlTransaction?> tx) : base(c, tx) { }

    public Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default) => GuardAsync<Customer?>(async () =>
    {
        const string sql = "SELECT Id, Email, FullName FROM dbo.Customers WHERE Id = @Id AND IsDeleted = 0";
        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    });

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default) => GuardAsync<Customer?>(async () =>
    {
        const string sql = "SELECT Id, Email, FullName FROM dbo.Customers WHERE Email = @Email AND IsDeleted = 0";
        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = email;
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    });

    public Task<bool> ExistsAsync(long id, CancellationToken ct = default) => GuardAsync<bool>(async () =>
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Customers WHERE Id = @Id AND IsDeleted = 0";
        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;
        var count = (int)(await command.ExecuteScalarAsync(ct) ?? 0);
        return count > 0;
    });

    public Task<IReadOnlyList<Customer>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        => GuardAsync<IReadOnlyList<Customer>>(async () =>
    {
        const string sql = @"
            SELECT Id, Email, FullName FROM dbo.Customers
            WHERE IsDeleted = 0
            ORDER BY Id
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";
        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Skip", SqlDbType.Int).Value = (page - 1) * pageSize;
        command.Parameters.Add("@Take", SqlDbType.Int).Value = pageSize;
        var result = new List<Customer>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(Map(reader));
        return result;
    });

    public Task<int> CountAsync(CancellationToken ct = default) => GuardAsync<int>(async () =>
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Customers WHERE IsDeleted = 0";
        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        return (int)(await command.ExecuteScalarAsync(ct) ?? 0);
    });

    public Task<long> AddAsync(Customer customer, CancellationToken ct = default) => GuardAsync<long>(async () =>
    {
        const string sql = @"
            INSERT INTO dbo.Customers (Email, FullName, CreatedBy)
            OUTPUT INSERTED.Id
            VALUES (@Email, @FullName, @CreatedBy)";
        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = customer.Email;
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 200).Value = customer.FullName;
        command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100).Value = "api";
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct));
    });

    public Task<bool> UpdateAsync(Customer customer, CancellationToken ct = default) => GuardAsync<bool>(async () =>
    {
        const string sql = @"
            UPDATE dbo.Customers
            SET Email = @Email, FullName = @FullName, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND IsDeleted = 0";
        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = customer.Id;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = customer.Email;
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 200).Value = customer.FullName;
        command.Parameters.Add("@UpdatedBy", SqlDbType.NVarChar, 100).Value = "api";
        return await command.ExecuteNonQueryAsync(ct) > 0;
    });

    public Task<bool> DeleteAsync(long id, CancellationToken ct = default) => GuardAsync<bool>(async () =>
    {
        const string sql = @"
            UPDATE dbo.Customers
            SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND IsDeleted = 0";
        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;
        command.Parameters.Add("@UpdatedBy", SqlDbType.NVarChar, 100).Value = "api";
        return await command.ExecuteNonQueryAsync(ct) > 0;
    });

    private static Customer Map(SqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Email = reader.GetString(1),
        FullName = reader.GetString(2)
    };
}
