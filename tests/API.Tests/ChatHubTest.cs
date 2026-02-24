using Microsoft.AspNetCore.SignalR;
using Moq;
using VoxChat.API.Hubs;
using VoxChat.API.Models;
using VoxChat.Application.Interfaces;
using VoxChat.Application.Models;

namespace API.Tests;

public class ChatHubTest
{

	private readonly ChatHub _chatHub; 

	private readonly Mock<IChatHubService> _chatHubServiceMock = new();
	
	private Mock<IHubCallerClients<IChatClient>> _clientsMock = new();
	private Mock<IChatClient> _callerMock = new();
	private Mock<IGroupManager> _groupsMock = new();
	private Mock<HubCallerContext> _contextMock = new();
	
	List<ChatMessage> chatlog = new([
		new("user1", "hello"),
		new("user2", "hi")
	]);

	List<string> chatMembers = new([
		"user1", "user2", "TestUser"
	]);

	private List<string> peerList = new([
		new Guid().ToString(),
		new Guid().ToString(),
		new Guid().ToString()
	]);
	
	string connectionId = "connection-id";
	UserConnection connection = new("TestUser", "TestChat");
	string groupName;
	
	public ChatHubTest()
	{
		
		_chatHub = new ChatHub(_chatHubServiceMock.Object)
		{
			Clients = _clientsMock.Object,
			Context = _contextMock.Object,
			Groups = _groupsMock.Object
		};
		
		groupName = connection.Chatroom;
		
		//context setup
		_contextMock.SetupGet(c => c.ConnectionId)
			.Returns(connectionId);
		
		_clientsMock.Setup(c => c.Client(connectionId))
			.Returns(_callerMock.Object);
		_clientsMock.Setup(c => c.Group(groupName))
			.Returns(_callerMock.Object);
		
		//needed for RetrieveChatLog
		_chatHubServiceMock.Setup(s=> s
				.GetGroupListAsync<ChatMessage>(groupName, ChatKeys.ChatLog))
			.ReturnsAsync(chatlog);
		//Needed for GetGroupName
		_chatHubServiceMock.Setup(s=> s
				.GetConnectionAsync(connectionId))
			.ReturnsAsync(connection);
		//Needed for RetrieveChatMembersList
		_chatHubServiceMock.Setup(s=> s
				.GetGroupListAsync<string>(groupName, ChatKeys.ChatMembers))
			.ReturnsAsync(chatMembers);
	}
	
	
	[Fact]
	public async Task JoinChat_SendsGreetMessage()
	{
		
		await _chatHub.JoinChat(connection);

		_clientsMock.Verify(c => c
			.Group(groupName)
			.ReceiveMessage(connection.Username, $"{connection.Username} joined the chatroom."));
	}

	[Fact]
	public async Task JoinChat_AddsUserToGroup()
	{
		await _chatHub.JoinChat(connection);

		
		_groupsMock.Verify(g => g
				.AddToGroupAsync(connectionId, groupName, new CancellationToken()),
			Times.Once);
	}
	
	[Fact]
	public async Task JoinChat_ConnectionIsSavedToCache()
	{
		await _chatHub.JoinChat(connection);

		
		_chatHubServiceMock.Verify(
			s => s.AddConnectionAsync(connectionId, connection),
			Times.Once);
	}
	
	[Fact]
	public async Task JoinChat_SendsChatLogToCaller()
	{
		await _chatHub.JoinChat(connection);

		
		_clientsMock.Verify(c => c
				.Client(connectionId)
				.GetChatLog(It.Is<List<ChatMessage>>(
					list => list.SequenceEqual(chatlog))), 
			Times.Once);

	}

		
	[Fact]
	public async Task JoinChat_SendsChatMembersListToCaller()
	{

		_chatHubServiceMock.Setup(s => s
			.AddItemToGroupListAsync(groupName, ChatKeys.ChatMembers, connection.Username))
			.ReturnsAsync(chatMembers);
		
		await _chatHub.JoinChat(connection);

		
		
		_clientsMock.Verify(c => c
				.Group(groupName)
				.GetChatMembersList(It.Is<List<string>>(
					list => list.SequenceEqual(chatMembers))), 
			Times.Once);

	}
	
	[Fact]
	public async Task SendPeer_SendsUpdatedPeerListToGroup()
	{
		string newPeerId = Guid.NewGuid().ToString();
		peerList.Add(newPeerId);
		
		_chatHubServiceMock.Setup(s=> s
				.AddItemToGroupListAsync(groupName, ChatKeys.Peers, newPeerId))
			.ReturnsAsync(peerList);
		
		await _chatHub.SendPeer(newPeerId);


		_clientsMock.Verify(c => c
			.Group(groupName)
			.ReceivePeer(peerList), Times.Once);
		
	}
	
	[Fact]
	public async Task SendPeer_SavesPeerToCache()
	{
		string newPeerId = Guid.NewGuid().ToString();
		peerList.Add(newPeerId);
		
		_chatHubServiceMock.Setup(s=> s
				.AddItemToGroupListAsync(groupName, ChatKeys.Peers, newPeerId))
			.ReturnsAsync(peerList);
		
		await _chatHub.SendPeer(newPeerId);


		_chatHubServiceMock.Verify(s => s
			.AddAsync(connectionId, ChatKeys.Peer, newPeerId));
	}

	[Fact]
	public async Task RetrievePeersList_ReturnsPeersList()
	{
		_chatHubServiceMock.Setup(s => s
				.GetGroupListAsync<string>(groupName, ChatKeys.Peers))
			.ReturnsAsync(peerList);
			
		List<string> list = await _chatHub.RetrievePeersList();
		
		Assert.Equal(peerList, list);
	}

	[Fact]
	public async Task RemovePeer_RemovesPeersFromGroup_SendsUpdatedPeerList()
	{

		string removedPeerId = peerList[0];
		peerList.RemoveAt(0);
		
		_chatHubServiceMock.Setup(s => s
			.RemoveItemFromGroupListAsync(groupName, ChatKeys.Peers, removedPeerId))
			.ReturnsAsync(peerList);
		
		await _chatHub.RemovePeer(removedPeerId);

		_chatHubServiceMock.Verify(s => s
			.RemoveAsync(connectionId, ChatKeys.Peer),  
			Times.Once);
		
		_clientsMock.Verify(c => c
			.Group(groupName)
			.ReceivePeer(peerList),
			Times.Once);
	}



	[Fact]
	public async Task SendMessage_CallsReceiveMessage()
	{
		string message = "Hello world";
		
		await _chatHub.SendMessage(message);

		_clientsMock.Verify(c => c
			.Group(groupName)
			.ReceiveMessage(connection.Username, message));
		
	}


	[Fact]
	public async Task OnDisconnectedAsync_RemovesEverythingNeeded()
	{
	
		chatMembers.Remove("TestUser");
		_chatHubServiceMock.Setup(s => s
				.RemoveItemFromGroupListAsync(groupName, ChatKeys.ChatMembers, connection.Username))
			.ReturnsAsync(chatMembers);


		string peerIdToDelete = peerList[0];
		peerList.RemoveAt(0);
		_chatHubServiceMock.Setup(s => s
			.RemoveItemFromGroupListAsync(groupName, ChatKeys.Peers, peerIdToDelete))
			.ReturnsAsync(peerList);
		_chatHubServiceMock.Setup(s => s
			.GetStringAsync(connectionId, ChatKeys.Peer))
			.ReturnsAsync(peerIdToDelete);
		
		await _chatHub.OnDisconnectedAsync(null);


		
		_clientsMock.Verify(c => c
			.Group(groupName)
			.ReceiveMessage(connection.Username,
				$"{connection.Username} left the chatroom.")
		, Times.Once);


		_clientsMock.Verify(c => c
			.Group(groupName)
			.GetChatMembersList(chatMembers)
		);
		
		_chatHubServiceMock.Verify(c => c
			.RemoveAsync(connectionId, ChatKeys.Peer));

		_groupsMock.Verify(g => g
			.RemoveFromGroupAsync(connectionId, groupName, new CancellationToken()));

		_chatHubServiceMock.Verify(c => c
			.RemoveItemFromGroupListAsync(groupName, ChatKeys.Peers, peerIdToDelete));
	}
	
	[Fact]
	public async Task OnDisconnectedAsync_WhenConnectionNull_DoesntThrowException()
	{
		connection = null;
		
		_chatHubServiceMock.Setup(s => s
			.GetConnectionAsync(connectionId))
			.ReturnsAsync(connection);
		
		await _chatHub.OnDisconnectedAsync(null);

	}
	
	
	
}
