using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using VoxChat.API.Models;
using VoxChat.Application.Models;
using VoxChat.Application.Services;

namespace Application.Tests;

public class ChatHubServiceTest
{

	private readonly Mock<IDistributedCache>  _cacheMock = new();
	private readonly ChatHubService _chatHubService;
	
	
	public ChatHubServiceTest()
	{
		_chatHubService = new ChatHubService(_cacheMock.Object);
		
	}
	
	
	[Fact]
	public async Task AddItemToGroupListAsync_ReturnsСorrectList()
	{
		
		string groupName = "TestGroup";
		ChatKeys key = ChatKeys.ChatMembers;
		string cacheKey = groupName + "-" + key.ToString().ToLower();

		
		List<string> chatMembers = new(["user grisha", "user pawel"]);
		string newChatMember = "Alexandro43";
		
		string chatMembersSerialized = JsonSerializer.Serialize(chatMembers);
		
		byte[] chatMembersSerializedBytes = Encoding.UTF8.GetBytes(chatMembersSerialized);
		_cacheMock.Setup(cache => cache.GetAsync(cacheKey, new CancellationToken()))
			.ReturnsAsync(() => chatMembersSerializedBytes);

		
		
		List<string> updatedList = await _chatHubService.AddItemToGroupListAsync(groupName, key, newChatMember);

		Assert.Contains(newChatMember, updatedList);
		
		Assert.Contains(chatMembers[0], updatedList);
		Assert.Contains(chatMembers[1], updatedList);
		Assert.True(updatedList.Count == 3);
		
	}
	
	[Fact]
	public async Task AddItemToGroupListAsync_AddsСorrectListToCache()
	{
		
		string groupName = "TestGroup";
		ChatKeys key = ChatKeys.ChatMembers;
		string cacheKey = groupName + "-" + key.ToString().ToLower();

		
		List<string> chatMembers = new(["user grisha", "user pawel"]);
		string newChatMember = "Alexandro43";
		
		string chatMembersSerialized = JsonSerializer.Serialize(chatMembers);
		
		byte[] chatMembersSerializedBytes = Encoding.UTF8.GetBytes(chatMembersSerialized);
		_cacheMock.Setup(cache => cache.GetAsync(cacheKey, new CancellationToken()))
			.ReturnsAsync(() => chatMembersSerializedBytes);

		
		
		List<string> updatedList = await _chatHubService.AddItemToGroupListAsync(groupName, key, newChatMember);

		_cacheMock.Verify(cache => cache.SetAsync(
				It.Is<string>(s => s == cacheKey), 
				It.Is<byte[]>(data => 
					JsonSerializer.Deserialize<List<string>>(Encoding.UTF8.GetString(data), new JsonSerializerOptions())!
						.SequenceEqual(updatedList)), 
				It.IsAny<DistributedCacheEntryOptions>(), 
				It.IsAny<CancellationToken>()), 
			Times.Once());
	}


	[Fact]
	public async Task AddItemToGroupListAsync_WhereListIsEmpty_ReturnsСorrectList()
	{

		string groupName = "TestGroup";
		ChatKeys key = ChatKeys.ChatMembers;
		string cacheKey = groupName + "-" + key.ToString().ToLower();
		
		string newChatMember = "Alexandro43";
		
		_cacheMock.Setup(cache => cache.GetAsync(cacheKey, new CancellationToken()))
			.ReturnsAsync(() => null);
		
		

		List<string> updatedList = await _chatHubService.AddItemToGroupListAsync(groupName, key, newChatMember);

		Assert.Contains(newChatMember, updatedList);
		Assert.True(updatedList.Count == 1);

	}
	
	
	public static IEnumerable<object[]> GetGenericData()
	{
		yield return new object[] { "group name",  ChatKeys.ChatMembers, "Alexandro43" };
		yield return new object[] { "group name",  ChatKeys.Peers, Guid.NewGuid() };
		yield return new object[] { "group name",  ChatKeys.Peer, Guid.NewGuid() };
		yield return new object[] { "group name",  ChatKeys.ChatLog, new ChatMessage("alexandro", "hellooooo") };
	}

	[Theory]
	[MemberData(nameof(GetGenericData))]
	public async Task AddItemToGroupListAsync_WhereDifferentChatKeys_ReturnsСorrectList<T>
		(string groupName, ChatKeys key, T item)
	{
		
		string cacheKey = groupName + "-" + key.ToString().ToLower();
		
		
		_cacheMock.Setup(cache => cache.GetAsync(cacheKey, new CancellationToken()))
			.ReturnsAsync(() => null);
		
		
		List<T> updatedList = await _chatHubService.AddItemToGroupListAsync(groupName, key, item);
		
		Assert.Contains(item, updatedList);
		Assert.True(updatedList.Count == 1);
		
	}
	
	[Theory]
	[MemberData(nameof(GetGenericData))]
	public async Task AddItemToGroupListAsync_WhereDifferentChatKeys_AddsCorrectListToCache<T>
		(string groupName, ChatKeys key, T item)
	{
		
		string cacheKey = groupName + "-" + key.ToString().ToLower();
		
		
		_cacheMock.Setup(cache => cache.GetAsync(cacheKey, new CancellationToken()))
			.ReturnsAsync(() => null);
		
		
		
		List<T> updatedList = await _chatHubService.AddItemToGroupListAsync(groupName, key, item);
		
		_cacheMock.Verify(cache => cache.SetAsync(
				It.Is<string>(s => s == cacheKey), 
				It.Is<byte[]>(data => 
					JsonSerializer.Deserialize<List<T>>(Encoding.UTF8.GetString(data), new JsonSerializerOptions())!
						.SequenceEqual(updatedList)), 
				It.IsAny<DistributedCacheEntryOptions>(), 
				It.IsAny<CancellationToken>()), 
			Times.Once());

	}


	[Fact]
	public async Task AddAsync_WhereKeyIsPeers_AddsCorrectItem()
	{
		
		string connectionId = "87r98qewuytrph3q2p";
		ChatKeys key = ChatKeys.Peer;
		
		string  cacheKey = connectionId + "-" + key.ToString().ToLower();

		string value = new Guid().ToString();
		await _chatHubService.AddAsync(connectionId, key, value);

		_cacheMock.Verify(cache => cache.SetAsync(
				It.Is<string>(s => s == cacheKey), 
				It.Is<byte[]>(data => 
					Encoding.UTF8.GetString(data).Equals(value)), 
				It.IsAny<DistributedCacheEntryOptions>(), 
				It.IsAny<CancellationToken>()), 
			Times.Once());
	}
	
	[Fact]
	public async Task RemoveAsync_WhereKeyIsPeers_RemovesCorrectItem()
	{
		
		string connectionId = "87r98qewuytrph3q2p";
		ChatKeys key = ChatKeys.Peer;
		
		string  cacheKey = connectionId + "-" + key.ToString().ToLower();

		await _chatHubService.RemoveAsync(connectionId, key);

		_cacheMock.Verify(cache => cache.RemoveAsync(
				It.Is<string>(s => s == cacheKey), 
				It.IsAny<CancellationToken>()), 
			Times.Once());
	}
	
	
	[Fact]
	public async Task RemoveItemFromGroupListAsync_ReturnsСorrectList()
	{
		
		string groupName = "TestGroup";
		ChatKeys key = ChatKeys.ChatMembers;
		string cacheKey = groupName + "-" + key.ToString().ToLower();

		
		List<string> chatMembers = new(["user grisha", "user pawel", "Alexandro43"]);
		string chatMemberToDelete = "Alexandro43";
		
		
		string chatMembersSerialized = JsonSerializer.Serialize(chatMembers);
		
		byte[] chatMembersSerializedBytes = Encoding.UTF8.GetBytes(chatMembersSerialized);
		_cacheMock.Setup(cache => cache.GetAsync(cacheKey, new CancellationToken()))
			.ReturnsAsync(() => chatMembersSerializedBytes);

		
		
		List<string> updatedList = await _chatHubService.RemoveItemFromGroupListAsync(groupName, key, chatMemberToDelete);

		Assert.DoesNotContain(chatMemberToDelete, updatedList);
		
		Assert.Contains(chatMembers[0], updatedList);
		Assert.Contains(chatMembers[1], updatedList);
		Assert.True(updatedList.Count == 2);
		
	}
	
	[Fact]
	public async Task RemoveItemFromGroupListAsync_RemovesItemFromCache()
	{
		
		string groupName = "TestGroup";
		ChatKeys key = ChatKeys.ChatMembers;
		string cacheKey = groupName + "-" + key.ToString().ToLower();

		
		List<string> chatMembers = new(["user grisha", "user pawel", "Alexandro43"]);
		string chatMemberToDelete = "Alexandro43";
		
		string chatMembersSerialized = JsonSerializer.Serialize(chatMembers);
		
		
		byte[] chatMembersSerializedBytes = Encoding.UTF8.GetBytes(chatMembersSerialized);
		_cacheMock.Setup(cache => cache.GetAsync(cacheKey, new CancellationToken()))
			.ReturnsAsync(() => chatMembersSerializedBytes);

		
		
		List<string> updatedList = await _chatHubService.RemoveItemFromGroupListAsync(groupName, key, chatMemberToDelete);

		_cacheMock.Verify(cache => cache.SetAsync(
				It.Is<string>(s => s == cacheKey), 
				It.Is<byte[]>(data => 
					JsonSerializer.Deserialize<List<string>>(Encoding.UTF8.GetString(data), new JsonSerializerOptions())!
						.SequenceEqual(updatedList)), 
				It.IsAny<DistributedCacheEntryOptions>(), 
				It.IsAny<CancellationToken>()), 
			Times.Once());
	}
	
	[Fact]
	public async Task GetGroupListAsync_ReturnsСorrectList()
	{
		
		string groupName = "TestGroup";
		ChatKeys key = ChatKeys.ChatMembers;
		string cacheKey = groupName + "-" + key.ToString().ToLower();

		
		List<string> chatMembers = new(["user grisha", "user pawel", "Alexandro43"]);
		
		
		string chatMembersSerialized = JsonSerializer.Serialize(chatMembers);
		
		byte[] chatMembersSerializedBytes = Encoding.UTF8.GetBytes(chatMembersSerialized);
		_cacheMock.Setup(cache => cache.GetAsync(cacheKey, new CancellationToken()))
			.ReturnsAsync(() => chatMembersSerializedBytes);

		
		
		List<string> updatedList = await _chatHubService.GetGroupListAsync<string>(groupName, key);

		
		Assert.Contains(chatMembers[0], updatedList);
		Assert.Contains(chatMembers[1], updatedList);
		Assert.Contains(chatMembers[2], updatedList);
		Assert.True(updatedList.Count == 3);
		
	}
	
	[Fact]
	public async Task GetGroupListAsync_WhenListIsNull_ReturnsEmptyList()
	{
		
		string groupName = "TestGroup";
		ChatKeys key = ChatKeys.ChatMembers;
		string cacheKey = groupName + "-" + key.ToString().ToLower();
		
		
		_cacheMock.Setup(cache => cache.GetAsync(cacheKey, new CancellationToken()))
			.ReturnsAsync(() => null);

		
		List<string> updatedList = await _chatHubService.GetGroupListAsync<string>(groupName, key);

		
		Assert.True(updatedList.Count == 0);
		
	}

	[Fact]
	public async Task GetStringAsync_WhereKeyIsPeer_ReturnsCorrectString()
	{
		string connectionId = "fgag7a6g879a#@!%";
		ChatKeys key = ChatKeys.Peer;
		Guid peerId = Guid.NewGuid(); 
		
		
		string peerIdSerialized = JsonSerializer.Serialize(peerId);
		byte[] peerIdSerializedBytes = Encoding.UTF8.GetBytes(peerIdSerialized);
		
		string cacheKey = connectionId + "-" + key.ToString().ToLower();
		_cacheMock.Setup(cache => cache.GetAsync(cacheKey, new CancellationToken()))
			.ReturnsAsync(() => peerIdSerializedBytes);
		

		string extractedPeerId = await _chatHubService.GetStringAsync(connectionId, key);
		
		
		Assert.True(extractedPeerId == peerIdSerialized);
	}
	
	[Fact]
	public async Task AddConnectionAsync_AddsCorrectConnection()
	{
		
		string connectionId = "87r98qewuytrph3q2p";
		UserConnection connection = new("Vasiliy", "chatroom512");
		
		string connectionSerialized = JsonSerializer.Serialize(connection);
		
		await _chatHubService.AddConnectionAsync(connectionId, connection);
		
		_cacheMock.Verify(cache => cache.SetAsync(
				It.Is<string>(s => s == connectionId), 
				It.Is<byte[]>(data => 
					Encoding.UTF8.GetString(data)
						.Equals(connectionSerialized)), 
				It.IsAny<DistributedCacheEntryOptions>(), 
				It.IsAny<CancellationToken>()), 
			Times.Once());
	}
	
	[Fact]
	public async Task GetConnectionAsync_ReturnsCorrectConnection()
	{
		
		string connectionId = "87r98qewuytrph3q2p";
		UserConnection connection = new("Vasiliy", "chatroom512");
		
		string connectionSerialized = JsonSerializer.Serialize(connection);
		
		byte[] connectionSerializedBytes = Encoding.UTF8.GetBytes(connectionSerialized);
		_cacheMock.Setup(cache => cache.GetAsync(connectionId, new CancellationToken()))
			.ReturnsAsync(() => connectionSerializedBytes);
		
		
		UserConnection receivedConnection = (await _chatHubService.GetConnectionAsync(connectionId))!;
		
		Assert.True(receivedConnection == connection);
	}
	
	[Fact]
	public async Task GetConnectionAsync_WhenConnectionIsNull_ReturnsNull()
	{
		
		string connectionId = "87r98qewuytrph3q2p";
		
		
		_cacheMock.Setup(cache => cache.GetAsync(connectionId, new CancellationToken()))
			.ReturnsAsync(() => null);


		
		UserConnection? receivedConnection = await _chatHubService.GetConnectionAsync(connectionId);
		
		Assert.True(receivedConnection == null);
	}


	[Fact]
	public async Task RemoveConnectionAsync_RemovesConnectionFromCache()
	{
		string connectionId = "87r98qewuytrph3q2p";
		
		await _chatHubService.RemoveConnectionAsync(connectionId);
		
		_cacheMock.Verify(cache => cache.RemoveAsync(
				connectionId, 
			It.IsAny<CancellationToken>()),  
			Times.Once() );
	}
	

	

}