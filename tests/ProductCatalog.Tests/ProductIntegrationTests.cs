using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;
using Xunit;

namespace ProductCatalog.Tests;

public class ProductIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProductIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Chaîne de connexion : priorité à la variable d'environnement (GitHub Actions),
        // sinon conteneur SQL Server local du TP1 (catalog-db sur localhost,1433)
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__CatalogDbConnection")
            ?? "Server=localhost,1433;Database=CatalogTestDb;User Id=sa;Password=DevOpsPass2026!#;TrustServerCertificate=True;";

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remplacer la configuration du DbContext de l'API par la BD de test
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(connectionString));
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task AddProduct_ShouldBeSavedInSqlServer()
    {
        // Arrange : nom court (16 caractères) pour respecter la limite de 30 de la colonne Name
        var uniqueName = "Prod-IT-" + Guid.NewGuid().ToString("N")[..8];

        var newProduct = new Product
        {
            Name = uniqueName,
            Price = 150.00m
        };

        // Act : envoyer le produit à l'API
        var response = await _client.PostAsJsonAsync("/api/products", newProduct);

        // Vérifier la réponse HTTP et afficher les détails en cas d'erreur
        var errorMessage = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Erreur HTTP : {response.StatusCode}. Détails : {errorMessage}"
        );

        // Lire le produit créé
        var createdProduct = await response.Content.ReadFromJsonAsync<Product>();

        Assert.NotNull(createdProduct);
        Assert.Equal(uniqueName, createdProduct.Name);

        // Assert : vérifier directement dans SQL Server
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var savedProduct = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == createdProduct.Id);

        Assert.NotNull(savedProduct);
        Assert.Equal(uniqueName, savedProduct.Name);
        Assert.Equal(150.00m, savedProduct.Price);
    }
}