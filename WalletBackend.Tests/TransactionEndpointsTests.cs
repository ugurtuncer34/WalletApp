using System.Net.Http.Json;
using FluentAssertions;
using WalletBackend.Dto;
using Xunit;

namespace WalletBackend.Tests;

public class TransactionEndpointsTests : IClassFixture<TestAppFactory>
{
    private readonly HttpClient _client;

    public TransactionEndpointsTests(TestAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_creates_and_Get_returns_transaction()
    {
        var dto = new
        {
            date = "2025-07-01",
            amount = 100m,
            direction = 1,  // Income
            accountId = 1,
            categoryId = 1
        };

        var post = await _client.PostAsJsonAsync("/transactions", dto);
        post.EnsureSuccessStatusCode();

        var created = await post.Content.ReadFromJsonAsync<TransactionReadDto>();
        created.Should().NotBeNull();

        var get = await _client.GetAsync($"/transactions/{created!.Id}");
        get.EnsureSuccessStatusCode();

        var fetched = await get.Content.ReadFromJsonAsync<TransactionReadDto>();
        fetched.Should().BeEquivalentTo(created);
    }
}