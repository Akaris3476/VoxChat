using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using VoxChat.API.Models;
using VoxChat.Application.Interfaces;
using VoxChat.Application.Models;

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

	private IChatHubService _chatHubService;
	
	public ChatHub(IChatHubService chatHubService)
	{
		_chatHubService = chatHubService;
	}
	
	
	
	public async Task JoinChat(UserConnection connection)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, connection.Chatroom);
		
		await _chatHubService.AddConnectionAsync(Context.ConnectionId, connection);
		
		await AddChatMember(connection.Username);

		await Clients.Client(Context.ConnectionId).GetChatLog(await RetrieveChatLog());
		await Clients.Group(connection.Chatroom).GetChatMembersList(await RetrieveChatMembersList());
		
		await ReceiveMessage(connection, $"{connection.Username} joined the chatroom.");
	}

	public async Task AddChatMember(string username)
	{
		string groupName = await GetGroupNameAsync();
		List<string> chatMembers = await _chatHubService.GetGroupListAsync<string>(groupName, ChatKeys.ChatMembers);


		await _chatHubService.AddItemToGroupListAsync(groupName, ChatKeys.ChatMembers, username);
		
	}

	public async Task RemoveChatMember(string username)
	{
		string groupName = await GetGroupNameAsync();
		List<string> chatMembers = await _chatHubService.RemoveItemFromGroupListAsync(groupName, ChatKeys.ChatMembers, username);

		
		if (chatMembers.Count > 0)
			await Clients.Group(await GetGroupNameAsync()).GetChatMembersList(chatMembers);
		
	}
	
	public async Task<List<string>> RetrieveChatMembersList()
	{
		
		string groupName = await GetGroupNameAsync();
		return await _chatHubService.GetGroupListAsync<string>(groupName, ChatKeys.ChatMembers);
		
	}
	
	private async Task<List<ChatMessage>> RetrieveChatLog()
	{
		
		string groupName = await GetGroupNameAsync();
		return await _chatHubService.GetGroupListAsync<ChatMessage>(groupName, ChatKeys.ChatLog);
		
	}

	private async Task<string> GetGroupNameAsync() 
		=> (await _chatHubService
			   .GetConnectionAsync(Context.ConnectionId))?.Chatroom ?? string.Empty;

	
	public async Task SendMessage(string message)
	{

		UserConnection? connection = await _chatHubService.GetConnectionAsync(Context.ConnectionId);
		
		if (connection is not null)
		{
			await ReceiveMessage(connection, message);
		}
		else
		{
			Console.WriteLine("connection is null(SendMessage method).");
		}
		
		
	}
	
	
	private async Task ReceiveMessage(UserConnection connection, string message)
	{
		try
		{
			string groupName = await GetGroupNameAsync();
			ChatMessage msg = new(connection.Username, message);
				
			await _chatHubService.AddItemToGroupListAsync(groupName, ChatKeys.ChatLog, msg);
			
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
		UserConnection? connection = await _chatHubService.GetConnectionAsync(Context.ConnectionId);
		
		if (connection is not null)
		{

			await AddPeer(peerId);
			
			Console.WriteLine(peerId);
			Console.WriteLine(connection.Chatroom);
			
		}
	}
	
	public async Task<List<string>> RetrievePeersList()
	{
		
		string groupName = await GetGroupNameAsync();
		return await _chatHubService.GetGroupListAsync<string>(groupName, ChatKeys.Peers);

	}
	
	public async Task AddPeer(string peerId)
	{
		await _chatHubService.AddAsync(Context.ConnectionId, ChatKeys.Peer, peerId);
		
		string groupName = await GetGroupNameAsync();
		await _chatHubService.GetGroupListAsync<string>(groupName, ChatKeys.Peers);


		 List<string> peerIds = await _chatHubService.AddItemToGroupListAsync(groupName, ChatKeys.Peers, peerId);
		
		if (peerIds.Count == 0)
			return;

		
		await Clients.Group(groupName)
			.ReceivePeer(peerIds);

	}

	public async Task RemovePeer(string peerId)
	{
		await _chatHubService.RemoveAsync(Context.ConnectionId, ChatKeys.Peer);

		string groupName = await GetGroupNameAsync();
		List<string> peers = await _chatHubService.RemoveItemFromGroupListAsync(groupName,  ChatKeys.Peers, peerId);
		

		if (peers.Count > 0)
			await Clients.Group(groupName)
				.ReceivePeer(peers);
		
	}
	
	
	
	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		try
		{

			UserConnection? connection = await _chatHubService.GetConnectionAsync(Context.ConnectionId);
			
			if (connection is null)
			{
				Console.WriteLine("failed to get UserConnection on disconnect");
			}
			else
			{
				
				string peerId = await _chatHubService.GetStringAsync(Context.ConnectionId, ChatKeys.Peer);

				if (!string.IsNullOrEmpty(peerId))
					await RemovePeer(peerId);
				
				
				await ReceiveMessage(connection, $"{connection.Username} left the chatroom.");

				await RemoveChatMember(connection.Username);
				
				await _chatHubService.RemoveConnectionAsync(Context.ConnectionId);

				await Groups.RemoveFromGroupAsync(Context.ConnectionId, connection.Chatroom);
				
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