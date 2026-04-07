using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface IOperatorSessionService
{
    Task<(OperatorSession? Session, string? RawToken)> AuthenticateAsync(string? sessionTokenHash);
    Task<(OperatorSession Session, string RawToken)> CreateAsync(int usuarioId, string? ipAddress, string? userAgent, string? deviceType, string? revokeReason);
    Task<(OperatorSession Session, string NewToken)> RefreshAsync(int sessionId, int usuarioId);
    Task<bool> ValidatePinAsync(int usuarioId, string pin);
    Task RevokeAsync(int sessionId, int usuarioId, string? reason);
    Task RevokeAllForUserAsync(int usuarioId, string? reason);
    Task<Usuario?> ValidateCredentialsAsync(string username, string password, string? pin);
}

public class OperatorSessionService : IOperatorSessionService
{
    private readonly ApplicationDbContext _context;
    private readonly IHashService _hashService;

    public OperatorSessionService(ApplicationDbContext context, IHashService hashService)
    {
        _context = context;
        _hashService = hashService;
    }

    public async Task<(OperatorSession? Session, string? RawToken)> AuthenticateAsync(string? sessionTokenHash)
    {
        if (string.IsNullOrEmpty(sessionTokenHash))
            return (null, null);

        var hash = _hashService.HashSha256(sessionTokenHash);
        var session = await _context.OperatorSessions
            .FirstOrDefaultAsync(s => s.SessionTokenHash == hash && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow);

        if (session == null)
            return (null, null);

        session.LastSeenAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (session, sessionTokenHash);
    }

    public async Task<(OperatorSession Session, string RawToken)> CreateAsync(int usuarioId, string? ipAddress, string? userAgent, string? deviceType, string? revokeReason)
    {
        var existingSessions = await _context.OperatorSessions
            .Where(s => s.UsuarioId == usuarioId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var session in existingSessions)
        {
            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokeReason = revokeReason ?? "New session created";
        }

        var rawToken = _hashService.GenerateToken();
        var tokenHash = _hashService.HashSha256(rawToken);

        var sessionEntity = new OperatorSession
        {
            UsuarioId = usuarioId,
            SessionTokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            LastSeenAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _context.OperatorSessions.Add(sessionEntity);
        await _context.SaveChangesAsync();

        return (sessionEntity, rawToken);
    }

    public async Task<(OperatorSession Session, string NewToken)> RefreshAsync(int sessionId, int usuarioId)
    {
        var session = await _context.OperatorSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UsuarioId == usuarioId);

        if (session == null)
            throw new InvalidOperationException("Session not found");

        if (session.IsRevoked || session.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Session is invalid or expired");

        var rawToken = _hashService.GenerateToken();
        session.SessionTokenHash = _hashService.HashSha256(rawToken);
        session.ExpiresAt = DateTime.UtcNow.AddHours(8);
        session.LastSeenAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (session, rawToken);
    }

    public async Task<bool> ValidatePinAsync(int usuarioId, string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
            return false;

        var user = await _context.Usuarios.FindAsync(usuarioId);
        if (user == null || !user.IsActive)
            return false;

        var pinHash = _hashService.HashSha256(pin.Trim());
        return user.PinHash == pinHash;
    }

    public async Task RevokeAsync(int sessionId, int usuarioId, string? reason)
    {
        var session = await _context.OperatorSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UsuarioId == usuarioId);

        if (session == null)
            throw new InvalidOperationException("Session not found");

        session.IsRevoked = true;
        session.RevokedAt = DateTime.UtcNow;
        session.RevokeReason = reason;

        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllForUserAsync(int usuarioId, string? reason)
    {
        var sessions = await _context.OperatorSessions
            .Where(s => s.UsuarioId == usuarioId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokeReason = reason;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<Usuario?> ValidateCredentialsAsync(string username, string password, string? pin)
    {
        var passwordHash = _hashService.HashSha256(password);
        
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == passwordHash && u.IsActive);

        if (usuario == null)
            return null;

        if (!string.IsNullOrEmpty(pin))
        {
            var pinHash = _hashService.HashSha256(pin);
            if (usuario.PinHash != pinHash)
                return null;
        }

        return usuario;
    }
}
