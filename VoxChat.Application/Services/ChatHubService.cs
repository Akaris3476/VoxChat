using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using VoxChat.API.Models;
using VoxChat.Application.Interfaces;
using VoxChat.Application.Models;

namespace VoxChat.Application.Services;

public class ChatHubService : IChatHubService
{

	private readonly IDistributedCache _cache;

	
	public DistributedCacheEntryOptions ChatLogExpirationOptions { get; } = new DistributedCacheEntryOptions()
		.SetAbsoluteExpiration(TimeSpan.FromMinutes(40));
	public DistributedCacheEntryOptions ChatMembersExpirationOptions { get; } = new DistributedCacheEntryOptions()
		.SetAbsoluteExpiration(TimeSpan.FromMinutes(180));
	public DistributedCacheEntryOptions ChatPeersExpirationOptions { get; } = new DistributedCacheEntryOptions()
		.SetAbsoluteExpiration(TimeSpan.FromMinutes(240));

	
	public ChatHubService(IDistributedCache cache)
	{
		_cache = cache;
	}

	

	public async Task<List<T>> AddItemToGroupListAsync<T>(string groupName, ChatKeys key, T item)
	{
		string listKey = key.ToString().ToLower();
		string? listSerialized =  await _cache.GetStringAsync(groupName + "-" + listKey);

		List<T> list;
		if (string.IsNullOrWhiteSpace(listSerialized))
		{
			list = await AddEmptyListAsync<T>(groupName, key);
		}
		else
		{
			list = JsonSerializer.Deserialize<List<T>>(listSerialized)!;
		}
		
		list.Add(item);
		
		listSerialized = JsonSerializer.Serialize(list);
		
		DistributedCacheEntryOptions expirationOptions = GetExpirationOptions(key);
		
		await _cache.SetStringAsync(groupName+ "-" +listKey, listSerialized, expirationOptions);
		
		return list;
	}
	
	public async Task AddAsync(string connectionId, ChatKeys key, string value)
	{
		string listKey = key.ToString().ToLower();
		
		DistributedCacheEntryOptions expirationOptions = GetExpirationOptions(key);
		await _cache.SetStringAsync(connectionId + "-" + listKey, value, expirationOptions);
	}

	private async Task<List<T>> AddEmptyListAsync<T>(string groupName, ChatKeys key)
	{
		List<T> emptyList = new();
		string emptyListSerialized = JsonSerializer.Serialize(emptyList);

		DistributedCacheEntryOptions expirationOptions = GetExpirationOptions(key);
		
		string listKey = key.ToString().ToLower();
		await _cache.SetStringAsync(groupName + "-" + listKey, emptyListSerialized, expirationOptions);
		
		return emptyList;
	}

	private DistributedCacheEntryOptions GetExpirationOptions(ChatKeys key)
	{
		DistributedCacheEntryOptions expirationOptions;
		switch (key)
		{
			case ChatKeys.ChatLog:
				expirationOptions = ChatLogExpirationOptions;
				break;
			
			case ChatKeys.ChatMembers:
				expirationOptions = ChatMembersExpirationOptions;
				break;
			
			case ChatKeys.Peer:
			case ChatKeys.Peers:
				expirationOptions = ChatPeersExpirationOptions;
				break;
			
			default:
				expirationOptions = ChatPeersExpirationOptions;
				break;
			
		}
		return  expirationOptions;
	}

	
	
	public async Task<List<T>> RemoveItemFromGroupListAsync<T>(string groupName, ChatKeys key, T item)
	{
		
		string listKey = key.ToString().ToLower();
		string? listSerialized =  await _cache.GetStringAsync(groupName + "-" + listKey);
		
		if (listSerialized is null)
			return await AddEmptyListAsync<T>(groupName, key);
		
		List<T> list = JsonSerializer.Deserialize<List<T>>(listSerialized)!;
		
		list.Remove(item);
		
		listSerialized = JsonSerializer.Serialize(list);
		
		DistributedCacheEntryOptions expirationOptions = GetExpirationOptions(key);
		
		await _cache.SetStringAsync(groupName+ "-" +listKey, listSerialized, expirationOptions);
		
		return list;
		
	}

	public async Task RemoveAsync(string connectionId, ChatKeys key)
	{
		string listKey = key.ToString().ToLower();
		await _cache.RemoveAsync(connectionId + "-" + listKey);
	}

	
	public async Task<List<T>> GetGroupListAsync<T>(string groupName, ChatKeys key)
	{
		string listKey = key.ToString().ToLower();
		string? listSerialized =  await _cache.GetStringAsync(groupName + "-" + listKey);

		if (listSerialized is null)
			return await AddEmptyListAsync<T>(groupName, key);
		
		return JsonSerializer.Deserialize<List<T>>(listSerialized)!;
		
	}
	
	public async Task<string> GetStringAsync(string connectionId, ChatKeys key)
	{
		string listKey = key.ToString().ToLower();
		string? value = await _cache.GetStringAsync(connectionId + "-" + listKey);
		
		return value ?? string.Empty;
		
	}
	
	
	
	
	
	public async Task AddConnectionAsync(string connectionId, UserConnection connection)
	{
		string connectionSerialized = JsonSerializer.Serialize(connection);
		await _cache.SetStringAsync(connectionId, connectionSerialized);
		
		
	}

	public async Task<UserConnection?> GetConnectionAsync(string connectionId)
	{
		
		string? connectionSerialized = await _cache.GetStringAsync(connectionId);
		
		UserConnection? connection;

		if (connectionSerialized is null)
			return null;
		
		connection = JsonSerializer.Deserialize<UserConnection>(connectionSerialized);

		return connection;

	}

	public async Task RemoveConnectionAsync(string connectionId)
	{
		await _cache.RemoveAsync(connectionId);
	}
}