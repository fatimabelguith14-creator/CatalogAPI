
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductCatalog.Api.Data;
using ProductCatalog.Api.Models;

namespace ProductCatalog.Tests;

public class ProductUnitTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductUnitTests(WebApplicationFactory<Program> factory)
    {
        // Créer un nom unique pour la base de données
        var databaseName = "UnitTestingDb_" + Guid.NewGuid();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Supprimer la configuration SQL Server
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

                // Configurer la base de données en mémoire
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                });
            });
        });
    }

    // Test 1 : Ajouter un produit
    [Fact]
    public async Task AddProduct_ReturnsCreated_WhenProductIsValid()
    {
        var client = _factory.CreateClient();

        var newProduct = new Product
        {
            Name = "Clavier Mecanique",
            Price = 89.50m
        };

        var response = await client.PostAsJsonAsync("/api/products", newProduct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdProduct = await response.Content.ReadFromJsonAsync<Product>();

        Assert.NotNull(createdProduct);
        Assert.Equal("Clavier Mecanique", createdProduct.Name);
        Assert.Equal(89.50m, createdProduct.Price);
    }

    // Test 2 : Récupérer la liste des produits
    [Fact]
    public async Task GetAllProducts_ReturnsOkAndProductList()
    {
        var client = _factory.CreateClient();

        // Ajouter un produit avant de récupérer la liste
        await client.PostAsJsonAsync("/api/products", new Product
        {
            Name = "Ecran 27 pouces",
            Price = 250.00m
        });

        // Récupérer tous les produits
        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<Product>>();

        Assert.NotNull(products);
        Assert.NotEmpty(products);
        Assert.Contains(products, p => p.Name == "Ecran 27 pouces");
    }
}
