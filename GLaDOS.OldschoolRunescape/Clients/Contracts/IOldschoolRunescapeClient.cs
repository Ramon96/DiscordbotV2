using GLaDOS.OldschoolRunescape.Responses;

namespace GLaDOS.OldschoolRunescape.Clients.Contracts;

public interface IOldschoolRunescapeClient
{
    Task<OldschoolRunescapeHiscoreResponse> GetHiScoresByUsernameAsync(string username, CancellationToken cancellationToken = default);
}