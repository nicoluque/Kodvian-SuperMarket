using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;

namespace KodvianSuperMarket.Services;

public interface IHashService
{
    string HashSha256(string input);
    string GenerateToken();
}

public class HashService : IHashService
{
    public string HashSha256(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string GenerateToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public interface IRequestDeduplicationService
{
    IDisposable Acquire(string key);
}

public class RequestDeduplicationService : IRequestDeduplicationService
{
    private readonly ConcurrentDictionary<string, byte> _inFlight = new();

    public IDisposable Acquire(string key)
    {
        if (!_inFlight.TryAdd(key, 1))
            throw new InvalidOperationException("Duplicate submission in progress");

        return new RequestDeduplicationReleaser(key, _inFlight);
    }

    private sealed class RequestDeduplicationReleaser : IDisposable
    {
        private readonly string _key;
        private readonly ConcurrentDictionary<string, byte> _owner;
        private bool _disposed;

        public RequestDeduplicationReleaser(string key, ConcurrentDictionary<string, byte> owner)
        {
            _key = key;
            _owner = owner;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _owner.TryRemove(_key, out _);
            _disposed = true;
        }
    }
}
