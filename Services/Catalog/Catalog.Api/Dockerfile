# Збірка з КОРЕНЯ репозиторію:  docker build -f Services/Catalog/Catalog.Api/Dockerfile -t catalog-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY Services/Catalog/ Services/Catalog/
RUN dotnet restore Services/Catalog/Catalog.Api/Catalog.Api.csproj
RUN dotnet publish Services/Catalog/Catalog.Api/Catalog.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
# рядок підключення передається змінною оточення: ConnectionStrings__CatalogDb=...
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
