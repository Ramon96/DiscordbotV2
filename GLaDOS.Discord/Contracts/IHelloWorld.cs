namespace Glados.Discord.Contracts;

public interface IHelloWorld
{
    Task SayHelloAsync(CancellationToken cancellationToken = default);
}