using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using VoxChat.API.Models;

namespace VoxChat.API.Hubs;

public interface IChatClient
{
	public Task ReceiveMessage(string username, string message);
	public Task GetChatLog(List<ChatMessage> chatlog);
	public Task ReceivePeer(List<string> peerId);
	public Task GetChatMembersList(List<string> chatmembers);
}

public class ChatHub : Hub<IChatClient>
{
	private readonly IDistributedCache _cache;

	public DistributedCacheEntryOptions ChatExpirationOptions { get; } = new DistributedCacheEntryOptions()
		.SetAbsoluteExpiration(TimeSpan.FromMinutes(40));
	public DistributedCacheEntryOptions ChatMembersExpirationOptions { get; } = new DistributedCacheEntryOptions()
		.SetAbsoluteExpiration(TimeSpan.FromMinutes(180));
	
	
	public ChatHub(IDistributedCache cache)
	{
		_cache = cache;
	}
	
	
	
	public async Task JoinChat(UserConnection connection)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, connection.Chatroom);
		
		string stringConnection = JsonSerializer.Serialize(connection);
		await _cache.SetStringAsync(Context.ConnectionId, stringConnection);
		
		await AddChatMember(connection.Username);

		await Clients.Client(Context.ConnectionId).GetChatLog(await RetrieveChatLog());
		await Clients.Group(connection.Chatroom).GetChatMembersList(await RetrieveChatMembersList());
		
		await ReceiveMessage(connection, $"{connection.Username} joined the chatroom.");
	}

	public async Task AddChatMember(string username)
	{
		string? chatMembers =  await _cache.GetStringAsync(await  GetGroupNameAsync() + "-chatmembers");
		
		if (chatMembers is null)
		{
			List<string> initialList = new List<string>();
			initialList.Add(username);
			string initialListSerialized = JsonSerializer.Serialize(initialList);
			await _cache.SetStringAsync(await  GetGroupNameAsync() + "-chatmembers", initialListSerialized, ChatMembersExpirationOptions);
			return;
		}

		List<string> chatMembersList = JsonSerializer.Deserialize<List<string>>(chatMembers)!;
		chatMembersList.Add(username);
		
		string chatMembersSerialized = JsonSerializer.Serialize(chatMembersList);
		await _cache.SetStringAsync(await  GetGroupNameAsync() + "-chatmembers", chatMembersSerialized, ChatMembersExpirationOptions);

	}

	public async Task RemoveChatMember(string username)
	{
		string? chatMembers = await _cache.GetStringAsync(await  GetGroupNameAsync() + "-chatmembers");

		if (chatMembers is not null)
		{
			List<string> chatMembersList = JsonSerializer.Deserialize<List<string>>(chatMembers)!;
			chatMembersList.Remove(username);

			string chatMembersSerialized = JsonSerializer.Serialize(chatMembersList);
			await _cache.SetStringAsync(await  GetGroupNameAsync() + "-chatmembers", chatMembersSerialized, ChatMembersExpirationOptions);
			
			await Clients.Group(await GetGroupNameAsync()).GetChatMembersList(chatMembersList);


		}
		
	}
	
	public async Task<List<string>> RetrieveChatMembersList()
	{
		string? chatMembers =  await _cache.GetStringAsync(await  GetGroupNameAsync() + "-chatmembers");

		if (chatMembers is null)
		{
			string emptyList = JsonSerializer.Serialize(new List<string>());
			await _cache.SetStringAsync(await  GetGroupNameAsync() + "-chatmembers", emptyList, ChatMembersExpirationOptions);
			return new List<string>();
		}
		
		return JsonSerializer.Deserialize<List<string>>(chatMembers)!;
	}
	
	private async Task<List<ChatMessage>> RetrieveChatLog()
	{
		
		string? chatlog = await _cache.GetStringAsync(await GetGroupNameAsync() + "-chatlog");

		if (chatlog is null)
		{
			string emptyList = JsonSerializer.Serialize(new List<ChatMessage>()); 
			await _cache.SetStringAsync(await GetGroupNameAsync() + "-chatlog", emptyList, ChatExpirationOptions);
			return new List<ChatMessage>();
		}
		
		
		return JsonSerializer.Deserialize<List<ChatMessage>>(chatlog)!;
	}

	private async Task<string> GetGroupNameAsync()
	{
		string? stringConnection = await _cache.GetStringAsync(Context.ConnectionId);
		
		UserConnection? connection;
		try
		{
			connection = JsonSerializer.Deserialize<UserConnection>(stringConnection!);
		}
		catch (Exception)
		{
			connection = null;
		}
		
		if (connection is not null)
		{
			return connection.Chatroom;
		}
		else
		{
			return string.Empty;
		}
		
	}
	
	public async Task SendMessage(string message)
	{

		UserConnection? connection = await GetCachedUserConnection();
		
		if (connection is not null)
		{
			await ReceiveMessage(connection, message);
		}
		else
		{
			Console.WriteLine("connection is null(SendMessage method).");
		}
		
		
	}

	private async Task<UserConnection?> GetCachedUserConnection()
	{
		string? stringConnection = await _cache.GetStringAsync(Context.ConnectionId);
		
		UserConnection? connection;
		try
		{
			connection = JsonSerializer.Deserialize<UserConnection>(stringConnection!);
		}
		catch (Exception)
		{
			connection = null;
		}
		
		return connection;
	}
	
	private async Task ReceiveMessage(UserConnection connection, string message)
	{
		try
		{
			string chatlog = (await _cache.GetStringAsync(await GetGroupNameAsync() + "-chatlog"))!;
			
			List<ChatMessage> chatMessages = JsonSerializer.Deserialize<List<ChatMessage>>(chatlog)!;
			chatMessages.Add(new ChatMessage(connection.Username, message));
			chatlog = JsonSerializer.Serialize(chatMessages);
			
			await _cache.SetStringAsync(await GetGroupNameAsync() + "-chatlog", chatlog, ChatExpirationOptions);
		}
		catch (Exception e)
		{
			Console.WriteLine("Error getting chatlog");
			Console.WriteLine(e);
		}

		
		await  Clients.Group(connection.Chatroom)
			.ReceiveMessage(connection.Username, message);
	}


	public async Task SendPeer(string peerId)
	{
		UserConnection? connection = await GetCachedUserConnection();
		
		if (connection is not null)
		{

			await AddPeer(peerId);
			
			Console.WriteLine(peerId);
			Console.WriteLine(connection.Chatroom);
			
		}
	}
	
	public async Task<List<string>> RetrievePeersList()
	{
		string? peers =  await _cache.GetStringAsync(await  GetGroupNameAsync() + "-peers");

		if (peers is null)
		{
			string emptyList = JsonSerializer.Serialize(new List<string>());
			await _cache.SetStringAsync(await  GetGroupNameAsync() + "-peers", emptyList, ChatMembersExpirationOptions);
			return new List<string>();
		}
		
		return JsonSerializer.Deserialize<List<string>>(peers)!;
	}
	
	public async Task AddPeer(string peerId)
	{
		await _cache.SetStringAsync(Context.ConnectionId + "-peer", peerId, ChatMembersExpirationOptions);
		
		string? peerIds =  await _cache.GetStringAsync(await  GetGroupNameAsync() + "-peers");
		
		if (peerIds is null)
		{
			List<string> initialList = new List<string>();
			initialList.Add(peerId);
			string initialListSerialized = JsonSerializer.Serialize(initialList);
			await _cache.SetStringAsync(await  GetGroupNameAsync() + "-peers", initialListSerialized, ChatMembersExpirationOptions);
			return;
		}

		List<string> peersList = JsonSerializer.Deserialize<List<string>>(peerIds)!;
		peersList.Add(peerId);
		
		string peersSerialized = JsonSerializer.Serialize(peersList);
		await _cache.SetStringAsync(await  GetGroupNameAsync() + "-peers", peersSerialized, ChatMembersExpirationOptions);
		
		await Clients.Group(await GetGroupNameAsync())
			.ReceivePeer(peersList);

	}

	public async Task RemovePeer(string peerId)
	{
		await _cache.RemoveAsync(Context.ConnectionId + "-peer");

		string? peerIds = await _cache.GetStringAsync(await  GetGroupNameAsync() + "-peers");

		if (peerIds is not null)
		{
			List<string> peersList = JsonSerializer.Deserialize<List<string>>(peerIds)!;
			peersList.Remove(peerId);

			string peersSerialized = JsonSerializer.Serialize(peersList);
			await _cache.SetStringAsync(await  GetGroupNameAsync() + "-peers", peersSerialized, ChatMembersExpirationOptions);
			
			await Clients.Group(await GetGroupNameAsync())
				.ReceivePeer(peersList);


		}
		
	}
	
	
	
	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		try
		{
			string? stringConnection = await _cache.GetStringAsync(Context.ConnectionId);

			
			if (stringConnection is null)
			{
				Console.WriteLine("failed to exctract stringConnection on disconnect");
			}
			else
			{
				UserConnection? connection = JsonSerializer.Deserialize<UserConnection>(stringConnection);

				if (connection is not null)
				{
					string? peerId = await _cache.GetStringAsync(Context.ConnectionId + "-peer");

					if (peerId is not null)
						await RemovePeer(peerId);
					
					
					await ReceiveMessage(connection, $"{connection.Username} left the chatroom.");

					await RemoveChatMember(connection.Username);
					
					await _cache.RemoveAsync(Context.ConnectionId);

					await Groups.RemoveFromGroupAsync(Context.ConnectionId, connection.Chatroom);



				}
			}
		}
		finally
		{
			await base.OnDisconnectedAsync(exception);
			Console.WriteLine("Client disconnected: " + Context.ConnectionId);
		}
		
		
		

	}
	
	public override Task OnConnectedAsync()
	{
		Console.WriteLine("Client connected: " + Context.ConnectionId);
		return base.OnConnectedAsync();
	}
	
	
}