using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace API.IntegrationTests;

public class IntegrationTestWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
	private readonly RedisContainer _redisContainer = new RedisBuilder("redis:latest").Build();
	
	
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.ConfigureTestServices(services =>
		{
			var descriptor = services
				.SingleOrDefault(
					s => s.ServiceType == typeof(IDistributedCache));
			
			if (descriptor != null)
				services.Remove(descriptor);

			services.AddStackExchangeRedisCache(options =>
			{
				options.Configuration = _redisContainer.GetConnectionString();
				options.InstanceName = "test";
			});
		});
		
	}


	public Task InitializeAsync()
	{
		return _redisContainer.StartAsync();
	}

	public new async  Task DisposeAsync()
	{
		await base.DisposeAsync();
		await _redisContainer.StopAsync();
	}
}