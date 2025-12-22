using GLaDOS.OldschoolRunescape.Responses;
using GLaDOS.OldschoolRunescape.Requests;

namespace GLaDOS.OldschoolRunescape.Clients.Contracts;

public interface IOldschoolRunescapeClient
{
    Task<OldschoolRunescapeHiscoreResponse?> GetHiScoresByUsernameAsync(OldschoolRunescapeHiscoreRequest request, CancellationToken cancellationToken = default);
}