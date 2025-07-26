// tests/WalletBackend.UnitTests/PositiveAmountFilterTests.cs
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Moq;

using WalletBackend.Filters;
using WalletBackend.Dto;
using WalletBackend.Models;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;                 // BadRequestObjectResult
using Microsoft.AspNetCore.Routing;            // Endpoint
using Microsoft.AspNetCore.Http.Features;      // IEndpointFeature

namespace WalletBackend.Tests;

public class PositiveAmountFilterTests
{
    [Fact]
    public async Task Filter_returns_BadRequest_when_amount_negative()
    {
        // ----- arrange ----------------------------------------------------
        var dto = new TransactionCreateDto(
            Date: DateTime.UtcNow,
            Amount: -10m,
            Direction: TransactionDirection.Income,
            AccountId: 1,
            CategoryId: 1);

        // dummy endpoint just so HttpContext has one
        var endpoint = new Endpoint(_ => Task.CompletedTask,
                                    new EndpointMetadataCollection(),
                                    "test");

        var http = new DefaultHttpContext();
        // attach the endpoint (needed because the filter reads it internally)
        http.Features.Set<IEndpointFeature>(new EndpointFeature { Endpoint = endpoint });

        // mock the abstract context
        var ctxMock = new Mock<EndpointFilterInvocationContext>();
        ctxMock.SetupGet(c => c.Arguments).Returns(new object[] { dto });
        ctxMock.SetupGet(c => c.HttpContext).Returns(http);
        var ctx = ctxMock.Object;

        var filter = new PositiveAmountFilter();

        // ----- act --------------------------------------------------------
        var result = await filter.InvokeAsync(
            ctx,
            next: _ => ValueTask.FromResult<object?>(Results.Ok())   // would run if filter passes
        );

        // ----- assert -----------------------------------------------------
        result.Should().BeOfType<BadRequest<string>>()
              .Which.Value.Should().Be("Amount must be positive");
    }

    // tiny private helper so we can set the Endpoint on HttpContext
    private sealed class EndpointFeature : IEndpointFeature
    {
        public Endpoint? Endpoint { get; set; }
    }
}