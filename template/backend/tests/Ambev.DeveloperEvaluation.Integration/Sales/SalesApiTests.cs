using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Application.Sales;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

public class SalesApiTests : IClassFixture<SalesApiFactory>
{
    private readonly SalesApiFactory _factory;
    private HttpClient Client => _factory.Client;

    public SalesApiTests(SalesApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(3, 0, 300)]
    [InlineData(4, 40, 360)]
    [InlineData(9, 90, 810)]
    [InlineData(10, 200, 800)]
    [InlineData(20, 400, 1600)]
    public async Task Create_ShouldPersistDiscountAndReturnLocation(
        int quantity, decimal discount, decimal total)
    {
        var input = NewSale();
        input.Items[0].Quantity = quantity;
        using var response = await Client.PostAsJsonAsync("/api/sales", input);
        await AssertStatus(response, HttpStatusCode.Created);
        Assert.NotNull(response.Headers.Location);

        var sale = await Client.GetFromJsonAsync<SaleResult>(response.Headers.Location);
        Assert.NotNull(sale);
        Assert.Equal(total, sale.TotalAmount);
        Assert.Equal(discount, Assert.Single(sale.Items).Discount);
        Assert.False(sale.IsCancelled);
    }

    [Fact]
    public async Task Update_ShouldReplaceItemsAndPersistNewTotal()
    {
        var input = NewSale();
        var created = await Create(input);
        input.Items[0].Quantity = 10;

        using var updated = await Client.PutAsJsonAsync($"/api/sales/{created.Id}", input);
        await AssertStatus(updated, HttpStatusCode.OK);

        var sale = await Get(created.Id);
        Assert.Equal(800m, sale.TotalAmount);
        Assert.Equal(200m, Assert.Single(sale.Items).Discount);
        Assert.Equal(1, await _factory.CountItemsAsync(created.Id));

        input.Items[0].Quantity = 21;
        using var rejected = await Client.PutAsJsonAsync($"/api/sales/{created.Id}", input);
        await AssertStatus(rejected, HttpStatusCode.BadRequest);
        Assert.Equal(800m, (await Get(created.Id)).TotalAmount);
    }

    [Fact]
    public async Task CancelAndDelete_ShouldPreserveThenRemoveSaleAndItems()
    {
        var input = NewSale();
        var created = await Create(input);
        var route = $"/api/sales/{created.Id}";

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var cancelled = await Client.PatchAsync($"{route}/cancel", null);
            await AssertStatus(cancelled, HttpStatusCode.OK);
        }

        var sale = await Get(created.Id);
        Assert.True(sale.IsCancelled);
        Assert.Equal(360m, sale.TotalAmount);
        Assert.Single(sale.Items);

        using var update = await Client.PutAsJsonAsync(route, input);
        await AssertStatus(update, HttpStatusCode.Conflict);
        using var deleted = await Client.DeleteAsync(route);
        await AssertStatus(deleted, HttpStatusCode.NoContent);
        Assert.Equal(0, await _factory.CountItemsAsync(created.Id));
        using var missing = await Client.GetAsync(route);
        await AssertStatus(missing, HttpStatusCode.NotFound);
        using var missingCancellation = await Client.PatchAsync($"{route}/cancel", null);
        await AssertStatus(missingCancellation, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldRejectDuplicateNumberAndRepeatedProducts()
    {
        var input = NewSale();
        await Create(input);
        using var duplicate = await Client.PostAsJsonAsync("/api/sales", input);
        await AssertStatus(duplicate, HttpStatusCode.Conflict);

        input.SaleNumber = Guid.NewGuid().ToString();
        input.Items.Add(input.Items[0]);
        using var repeatedProduct = await Client.PostAsJsonAsync("/api/sales", input);
        await AssertStatus(repeatedProduct, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_ShouldFilterSortAndPaginateInDatabase()
    {
        var prefix = Guid.NewGuid().ToString("N");
        foreach (var quantity in new[] { 3, 4, 10 })
        {
            var input = NewSale();
            input.SaleNumber = $"{prefix}-{quantity}";
            input.Items[0].Quantity = quantity;
            await Create(input);
        }

        var query = $"/api/sales?saleNumber={prefix}*&_size=1&_order=totalAmount%20desc";
        var first = await Client.GetFromJsonAsync<SalePage>($"{query}&_page=1");
        var second = await Client.GetFromJsonAsync<SalePage>($"{query}&_page=2");
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(3, first.TotalItems);
        Assert.Equal(3, first.TotalPages);
        Assert.Equal(800m, Assert.Single(first.Data).TotalAmount);
        Assert.Equal(360m, Assert.Single(second.Data).TotalAmount);
        Assert.NotEqual(first.Data[0].Id, second.Data[0].Id);

        var filtered = await Client.GetFromJsonAsync<SalePage>(
            $"{query}&_minTotalAmount=700&_maxTotalAmount=900&customerName=Cliente*&isCancelled=false");
        Assert.NotNull(filtered);
        Assert.Equal(1, filtered.TotalItems);
        Assert.Equal(800m, Assert.Single(filtered.Data).TotalAmount);

        var dated = await Client.GetFromJsonAsync<SalePage>(
            $"{query}&_minSaleDate=2026-09-22T00:00:00Z");
        Assert.NotNull(dated);
        Assert.Empty(dated.Data);
        Assert.Equal(0, dated.TotalItems);
    }

    [Theory]
    [InlineData("_page=0")]
    [InlineData("_size=101")]
    [InlineData("_order=unknown")]
    [InlineData("_order=totalAmount%20invalid")]
    [InlineData("_minTotalAmount=900&_maxTotalAmount=700")]
    [InlineData("_minSaleDate=invalid")]
    public async Task List_ShouldRejectInvalidQuery(string query)
    {
        using var response = await Client.GetAsync($"/api/sales?{query}");
        await AssertStatus(response, HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ValidationError", error.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(21)]
    public async Task Create_ShouldRejectInvalidQuantity(int quantity)
    {
        var input = NewSale();
        input.Items[0].Quantity = quantity;
        using var response = await Client.PostAsJsonAsync("/api/sales", input);
        await AssertStatus(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldRejectNullItems()
    {
        var input = NewSale();
        input.Items = null!;
        using var response = await Client.PostAsJsonAsync("/api/sales", input);
        await AssertStatus(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_ShouldCombineRepeatedFiltersWithOrAndDifferentFiltersWithAnd()
    {
        var prefix = Guid.NewGuid().ToString("N");
        var ids = new List<Guid>();
        var customers = new List<Guid>();
        foreach (var name in new[] { "Alpha", "Beta", "Gamma" })
        {
            var input = NewSale();
            input.SaleNumber = $"{prefix}-{name}";
            input.CustomerName = name;
            customers.Add(input.CustomerId);
            ids.Add((await Create(input)).Id);
        }

        var page = await Client.GetFromJsonAsync<SalePage>(
            $"/api/sales?saleNumber={prefix}*&customerName=Alpha&customerName=Beta&_order=customerName%20asc");
        Assert.NotNull(page);
        Assert.Equal(2, page.TotalItems);
        Assert.Equal(new[] { ids[0], ids[1] }, page.Data.Select(sale => sale.Id));

        var byId = await Client.GetFromJsonAsync<SalePage>(
            $"/api/sales?saleNumber={prefix}*&customerId={customers[0]}&customerId={customers[1]}");
        Assert.NotNull(byId);
        Assert.Equal(2, byId.TotalItems);

        using var cancelled = await Client.PatchAsync($"/api/sales/{ids[0]}/cancel", null);
        await AssertStatus(cancelled, HttpStatusCode.OK);
        var active = await Client.GetFromJsonAsync<SalePage>(
            $"/api/sales?saleNumber={prefix}*&isCancelled=false");
        Assert.NotNull(active);
        Assert.Equal(2, active.TotalItems);
        Assert.DoesNotContain(active.Data, sale => sale.Id == ids[0]);

        var both = await Client.GetFromJsonAsync<SalePage>(
            $"/api/sales?saleNumber={prefix}*&isCancelled=false&isCancelled=true");
        Assert.NotNull(both);
        Assert.Equal(3, both.TotalItems);
    }

    [Fact]
    public async Task List_ShouldTreatSqlWildcardCharactersAsLiteralText()
    {
        var prefix = Guid.NewGuid().ToString("N");
        var exact = NewSale();
        exact.SaleNumber = $"{prefix}_100%";
        var created = await Create(exact);
        var other = NewSale();
        other.SaleNumber = $"{prefix}X100Y";
        await Create(other);

        var page = await Client.GetFromJsonAsync<SalePage>(
            $"/api/sales?saleNumber={Uri.EscapeDataString(exact.SaleNumber)}");
        Assert.NotNull(page);
        Assert.Equal(created.Id, Assert.Single(page.Data).Id);
    }

    [Fact]
    public async Task Swagger_ShouldDocumentCancellationErrors()
    {
        var document = await Client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var responses = document.GetProperty("paths")
            .GetProperty("/api/sales/{id}/cancel").GetProperty("patch").GetProperty("responses");
        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("404", out _));
    }

    private async Task<SaleResult> Create(SaleInput input)
    {
        using var response = await Client.PostAsJsonAsync("/api/sales", input);
        await AssertStatus(response, HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<SaleResult>())!;
    }

    private async Task<SaleResult> Get(Guid id)
    {
        using var response = await Client.GetAsync($"/api/sales/{id}");
        await AssertStatus(response, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<SaleResult>())!;
    }

    private static async Task AssertStatus(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.True(response.StatusCode == expected,
            $"Expected {expected}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    private static SaleInput NewSale() => new()
    {
        SaleNumber = Guid.NewGuid().ToString(),
        SaleDate = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc),
        CustomerId = Guid.NewGuid(),
        CustomerName = "Cliente Integracao",
        BranchId = Guid.NewGuid(),
        BranchName = "Filial Integracao",
        Items =
        [
            new SaleItemInput
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Produto Integracao",
                Quantity = 4,
                UnitPrice = 100m
            }
        ]
    };
}
