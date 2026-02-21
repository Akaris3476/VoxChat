using VoxChat.API.Models;
using VoxChat.Application.Models;

namespace VoxChat.Application.Interfaces;

public interface IChatHubService
{
	Task<List<T>> AddItemToGroupListAsync<T>(string groupName, ChatKeys key, T item);
	Task AddAsync(string connectionId, ChatKeys key, string value);
	Task<List<T>> RemoveItemFromGroupListAsync<T>(string groupName, ChatKeys key, T item);
	Task RemoveAsync(string connectionId, ChatKeys key);
	Task<List<T>> GetGroupListAsync<T>(string groupName, ChatKeys listKey);
	Task<string> GetStringAsync(string connectionId, ChatKeys key);

	Task AddConnectionAsync(string connectionId, UserConnection connection);
	Task<UserConnection?> GetConnectionAsync(string connectionId);
	Task RemoveConnectionAsync(string connectionId);
}