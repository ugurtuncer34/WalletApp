namespace WalletBackend.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHealthChecksWithUI(this IServiceCollection services)
    {
        services
                .AddHealthChecks()
                  .AddDbContextCheck<WalletDbContext>(
                    name: "Database",
                    tags: ["ready"]);

        services
            .AddHealthChecksUI(options =>
            {
                options.SetEvaluationTimeInSeconds(600);
                options.AddHealthCheckEndpoint("wallet-api", "/health");
            })
            .AddInMemoryStorage();
            
        return services;
    }
}