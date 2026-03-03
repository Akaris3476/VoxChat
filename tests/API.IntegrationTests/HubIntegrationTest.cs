using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using VoxChat.API.Models;
using VoxChat.Application.Interfaces;
using VoxChat.Application.Models;

namespace API.IntegrationTests;

public class HubIntegrationTest : IClassFixture<IntegrationTestWebFactory>
{
	private readonly IntegrationTestWebFactory _factory;

	public HubIntegrationTest(IntegrationTestWebFactory factory)
	{
		_factory = factory;
	}
	
	
	[Fact]
	public async Task JoinChat_ClientShouldGetChatLog()
	{
		//first client joins and sends a few messages
		var handler = _factory.Server.CreateHandler();
		var client = new HubConnectionBuilder()
			.WithUrl("http://localhost:5274/chat", o => o.HttpMessageHandlerFactory = _ => handler)
			.Build();
		
		UserConnection testUser = new("User1", "testroom");
		
		await client.StartAsync();
		await client.InvokeAsync("JoinChat", testUser);
		await client.InvokeAsync("SendMessage", "Hello1");
		await client.InvokeAsync("SendMessage", "message2");

		//second client joins and receives chatlog
		var client2 = new HubConnectionBuilder()
			.WithUrl("http://localhost:5274/chat", o => o.HttpMessageHandlerFactory = _ => handler)
			.Build();

		List<ChatMessage> chatlog = new();
		UserConnection testUser2 = new("User2", "testroom");
		
		await client2.StartAsync();
		
		client2.On<List<ChatMessage>>("GetChatLog", (chatloglist) 
			=> chatlog = new(chatloglist));
		
		await client2.InvokeAsync("JoinChat", testUser2);
		

		
		await Task.Delay(500);
		chatlog[0].Should().Be(new ChatMessage("User1", "User1 joined the chatroom."));
		chatlog[1].Should().Be(new ChatMessage("User1", "Hello1"));
		chatlog[2].Should().Be(new ChatMessage("User1", "message2"));

	}
	
	[Fact]
	public async Task JoinChat_ClientsShouldGetChatMembersList()
	{
		var handler = _factory.Server.CreateHandler();
		var client = new HubConnectionBuilder()
			.WithUrl("http://localhost:5274/chat", o => o.HttpMessageHandlerFactory = _ => handler)
			.Build();
		
		UserConnection testUser = new("User1", "testroom2");
		List<string> chatMembers1 = new();
						
		client.On<List<string>>("GetChatMembersList", (chatmembers) 
			=> chatMembers1 = new(chatmembers));
		
		await client.StartAsync();
		await client.InvokeAsync("JoinChat", testUser);
		
		var client2 = new HubConnectionBuilder()
			.WithUrl("http://localhost:5274/chat", o => o.HttpMessageHandlerFactory = _ => handler)
			.Build();


		List<string> chatMembers2 = new();
		UserConnection testUser2 = new("User2", "testroom2");
		
		client2.On<List<string>>("GetChatMembersList", (chatmembers) 
			=> chatMembers2 = new(chatmembers));
		
		await client2.StartAsync();
		await client2.InvokeAsync("JoinChat", testUser2);
		

		
		await Task.Delay(500);
		chatMembers1[0].Should().Be("User1");
		chatMembers1[1].Should().Be("User2");
		chatMembers1.Count.Should().Be(2);
		chatMembers2[0].Should().Be("User1");
		chatMembers2[1].Should().Be("User2");
		chatMembers2.Count.Should().Be(2);
	}
	
	[Fact]
	public async Task SendMessage_ShouldReachClient()
	{
		var handler = _factory.Server.CreateHandler();
		var client = new HubConnectionBuilder()
			.WithUrl("http://localhost:5274/chat", o => o.HttpMessageHandlerFactory = _ => handler)
			.Build();
		
		UserConnection testUser = new("User1", "testroom3");
		
		await client.StartAsync();
		await client.InvokeAsync("JoinChat", testUser);
		
		List<string> receivedMessages = new();
		client.On<string, string>("ReceiveMessage", (_, msg) 
			=> receivedMessages.Add(msg));
		
		
		await client.InvokeAsync("SendMessage", "Hello World");

		
		await Task.Delay(500);
		receivedMessages.Should().Contain("Hello World");
	}

	
	[Fact]
	public async Task TwoClients_ShouldSeeEachOtherMessages()
	{
		var handler = _factory.Server.CreateHandler();
		var client = new HubConnectionBuilder()
			.WithUrl("http://localhost:5274/chat", o => o.HttpMessageHandlerFactory = _ => handler)
			.Build();
		
		UserConnection testUser = new("User1", "testroom4");
		
		await client.StartAsync();
		await client.InvokeAsync("JoinChat", testUser);


		var client2 = new HubConnectionBuilder()
			.WithUrl("http://localhost:5274/chat", o => o.HttpMessageHandlerFactory = _ => handler)
			.Build();
		
		UserConnection testUser2 = new("User2", "testroom4");

		await client2.StartAsync();
		await client2.InvokeAsync("JoinChat", testUser2);
		
		List<string> receivedMessages1 = new();
		List<string> receivedMessages2 = new();

		client.On<string, string>("ReceiveMessage", (_, msg) 
			=> receivedMessages1.Add(msg));
		client2.On<string, string>("ReceiveMessage", (_, msg) 
			=> receivedMessages2.Add(msg));
		
		
		await client.InvokeAsync("SendMessage", "Hello");
		await client2.InvokeAsync("SendMessage", "Greetings");

		
		
		await Task.Delay(500);
		receivedMessages1[0].Should().Be("Hello");
		receivedMessages1[1].Should().Be("Greetings");
		receivedMessages2[0].Should().Be("Hello");
		receivedMessages2[1].Should().Be("Greetings");

	}
	
	
	[Fact]
	public async Task TwoClients_ShouldGetCorrectPeerList()
	{
		List<string> peerList = new();
		List<string> peerList2 = new();
		
		var handler = _factory.Server.CreateHandler();
		var client = new HubConnectionBuilder()
			.WithUrl("http://localhost:5274/chat", o => o.HttpMessageHandlerFactory = _ => handler)
			.Build();
		
		UserConnection testUser = new("User1", "testroom5");
		
		await client.StartAsync();
		await client.InvokeAsync("JoinChat", testUser);
		client.On<List<string>>("ReceivePeer", (peers) 
			=> peerList = new(peers));
		string peer1 = "gdag32gh2qg";
		await client.InvokeAsync("SendPeer", peer1);
		
		await Task.Delay(200);

		var client2 = new HubConnectionBuilder()
			.WithUrl("http://localhost:5274/chat", o => o.HttpMessageHandlerFactory = _ => handler)
			.Build();
		
		UserConnection testUser2 = new("User2", "testroom5");

		await client2.StartAsync();
		await client2.InvokeAsync("JoinChat", testUser2);
		
		client.On<List<string>>("ReceivePeer", (peers) 
			=> peerList2 = new(peers));
		
		string peer2 = "gaehgewyh43";
		await client2.InvokeAsync("SendPeer", peer2);
		
		
		
		await Task.Delay(400);
		peerList.Count.Should().Be(2);
		peerList[0].Should().Be(peer1);
		peerList[1].Should().Be(peer2);
		peerList.Should().BeEquivalentTo(peerList2);

	}
	
	[Fact]
	public async Task Clinet_ShouldRemoveSavedData_OnDisconnect()
	{

		var handler = _factory.Server.CreateHandler();
		var client = new HubConnectionBuilder()
			.WithUrl("http://localhost:5274/chat", o => o.HttpMessageHandlerFactory = _ => handler)
			.Build();
		
		UserConnection testUser = new("User1", "testroom6");
		
		
		await client.StartAsync();
		await client.InvokeAsync("JoinChat", testUser);
		string connectionId = client.ConnectionId!;
		string peer1 = "gdag32gh2qg";
		await client.InvokeAsync("SendPeer", peer1);

		
		await client.StopAsync();
		
		
		await Task.Delay(400);
		
		var scope = _factory.Services.CreateScope();	
		var chatHubService = scope.ServiceProvider.GetRequiredService<IChatHubService>();
		
		List<string> chatMembers = await chatHubService
			.GetGroupListAsync<string>(testUser.Chatroom, ChatKeys.ChatMembers);
		chatMembers.Count.Should().Be(0);

		string peerId = await chatHubService
			.GetStringAsync(connectionId, ChatKeys.Peer);
		peerId.Should().BeEmpty();

		UserConnection? connection = await chatHubService
			.GetConnectionAsync(connectionId);
		connection.Should().BeNull();
		
		
	}
}