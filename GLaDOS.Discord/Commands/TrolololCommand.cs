using Discord;
using Discord.WebSocket;

namespace Glados.Discord.Commands;

public class TrolololCommand : IDiscordCommand
{
    private static readonly string[] NatoFlags =
    [
        "🇦🇱", "🇧🇪", "🇧🇬", "🇨🇦", "🇭🇷", "🇨🇿", "🇩🇰", "🇪🇪", "🇫🇷", "🇩🇪",
        "🇬🇷", "🇭🇺", "🇮🇸", "🇮🇹", "🇱🇻", "🇱🇹", "🇱🇺", "🇲🇪", "🇳🇱", "🇲🇰",
        "🇳🇴", "🇵🇱", "🇵🇹", "🇷🇴", "🇸🇰", "🇸🇮", "🇪🇸", "🇹🇷", "🇬🇧", "🇺🇸"
    ];

    public string Name => "trololol";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Roll a number between 0 and 10. If it's 7, a bomb hits a random NATO country!")
            .Build();
    }

    public Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        var roll = Random.Shared.Next(0, 11);
        
        if (roll == 7)
        {
            var targetFlag = NatoFlags[Random.Shared.Next(NatoFlags.Length)];
            var asciiArt = $@"
    💣
     \
      \
       \
        \
         \
          \
           \
            {targetFlag} 💥
";
            return command.RespondAsync($"**TROLOLOL!** You rolled a **{roll}**!\n```{asciiArt}```");
        }

        return command.RespondAsync($"You rolled a **{roll}**. Not 7... no bomb today. 😤");
    }
}