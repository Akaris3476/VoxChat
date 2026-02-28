using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using VoxChat.API.Models;

namespace API.IntegrationTests;

public class IntegrationTest : IClassFixture<IntegrationTestWebFactory>
{
	private readonly IntegrationTestWebFactory _factory;

	public IntegrationTest(IntegrationTestWebFactory factory)
	{

		_factory = factory;
		
	}
	
	[Fact]
	public async Task Hub_SendMessage_ShouldReachClient()
	{
		
		var handler = _factory.Server.CreateHandler();
		var hubConnection = new HubConnectionBuilder()
			.WithUrl("http://localhost:5274/chat", o => o.HttpMessageHandlerFactory = _ => handler)
			.Build();
		
		UserConnection testUser = new("User1", "testroom");
		
		await hubConnection.StartAsync();
		await hubConnection.InvokeAsync("JoinChat", testUser);
		
		// List<string> receivedMessages = new();
		
		
		hubConnection.On<string, string>("ReceiveMessage", (user, msg) 
			=> msg.Should().Be("Hello World"));
		
		await hubConnection.InvokeAsync("SendMessage", "Hello World");
		await hubConnection.StopAsync();
	}
	
}