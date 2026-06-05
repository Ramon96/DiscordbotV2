namespace GLaDOS.Scheduler.Application.Discord.Clients;

public record ShirtlessOldManImageResult(string? ImageUrl, string? Error);

public interface IShirtlessOldManImageService
{
    Task<ShirtlessOldManImageResult> GetRandomImageUrlAsync(CancellationToken cancellationToken = default);
}
