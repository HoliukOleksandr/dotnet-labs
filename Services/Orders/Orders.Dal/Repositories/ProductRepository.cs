using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Abstractions;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;

namespace Orders.Dal.Repositories;

/// <summary>Локальна копія довідника Каталогу (власник — CatalogDb): оновлюється подіями з Каталогу.</summary>
public sealed class ProductRepository : BaseDapperRepository<Product>, IProductRepository
{
    public ProductRepository(SqlConnection c, Func<SqlTransaction?> tx) : base(c, tx, "dbo.Products") { }

    public Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default)
        => GuardAsync<IReadOnlyList<Product>>(async () =>
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<Product>(Cmd(
            "SELECT Id, Name, Price, Stock FROM dbo.Products WHERE Id IN @Ids", new { Ids = ids }, ct: ct));
        return rows.ToList();
    });

    public override Task<long> AddAsync(Product product, CancellationToken ct = default) => GuardAsync<long>(async () =>
    {
        // Id не IDENTITY: його генерує Каталог (див. політику дублювання)
        await EnsureOpenAsync(ct);
        await Connection.ExecuteAsync(Cmd(
            "INSERT INTO dbo.Products (Id, Name, Price, Stock) VALUES (@Id, @Name, @Price, @Stock)",
            new { product.Id, product.Name, product.Price, product.Stock }, ct: ct));
        return product.Id;
    });

    public override Task<bool> UpdateAsync(Product product, CancellationToken ct = default) => GuardAsync<bool>(async () =>
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.ExecuteAsync(Cmd(
            "UPDATE dbo.Products SET Name = @Name, Price = @Price, Stock = @Stock, SyncedAt = SYSUTCDATETIME() WHERE Id = @Id",
            new { product.Id, product.Name, product.Price, product.Stock }, ct: ct));
        return rows > 0;
    });

    public Task DecreaseStockAsync(long productId, int quantity, CancellationToken ct = default) => GuardAsync(async () =>
    {
        await EnsureOpenAsync(ct);
        // умова Stock >= @Quantity робить перевірку й зменшення атомарними
        var rows = await Connection.ExecuteAsync(Cmd(
            "UPDATE dbo.Products SET Stock = Stock - @Quantity WHERE Id = @ProductId AND Stock >= @Quantity",
            new { ProductId = productId, Quantity = quantity }, ct: ct));
        if (rows > 0) return;

        var exists = await Connection.ExecuteScalarAsync<int>(Cmd(
            "SELECT COUNT(1) FROM dbo.Products WHERE Id = @ProductId", new { ProductId = productId }, ct: ct));
        if (exists == 0) throw new NotFoundException($"Product {productId} not found");
        throw new BusinessConflictException($"Not enough stock for product {productId}");
    });
}
