namespace GLaDOS.Scheduler.Application.Discord.Clients;

public interface IShirtlessOldManImageService
{
    Task<string?> GetRandomImageUrlAsync(CancellationToken cancellationToken = default);
}
