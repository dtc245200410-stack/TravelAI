using System.Collections.Concurrent;

namespace TravelAI.Services;

// Fail quickly rather than queueing interactive requests behind a loaded GPU.
public sealed class AIRequestGate
{
    private readonly ConcurrentDictionary<int, byte> _users = new();
    private readonly SemaphoreSlim _model = new(1, 1);
    public bool TryEnterUser(int userId) => _users.TryAdd(userId, 0);
    public bool IsUserBusy(int userId) => _users.ContainsKey(userId);
    public void ExitUser(int userId) => _users.TryRemove(userId, out _);
    public bool TryEnterModel() => _model.Wait(0);
    public void ExitModel() => _model.Release();
}
