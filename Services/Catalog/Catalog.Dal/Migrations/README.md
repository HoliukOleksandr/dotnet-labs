# Migrations

Сюди `dotnet ef` кладе файли міграцій. Початкову міграцію створіть командою з кореня репозиторію
(див. розділ «Лаб. 03» у README.md):

```
dotnet ef migrations add InitialCreate --project Services/Catalog/Catalog.Dal --startup-project Services/Catalog/Catalog.Api
```

Після цього тут з'являться `<дата>_InitialCreate.cs`, `<дата>_InitialCreate.Designer.cs` і `CatalogDbContextModelSnapshot.cs`.
Їх обов'язково додайте в Git.
