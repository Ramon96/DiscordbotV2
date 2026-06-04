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

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        var roll = Random.Shared.Next(0, 11);

        if (roll != 7)
        {
            await command.RespondAsync($"You rolled a **{roll}**. Not 7... no bomb today. :pensive:");
            return;
        }

        await command.DeferAsync();

        var targetFlag = NatoFlags[Random.Shared.Next(NatoFlags.Length)];

        var frames = new[]
        {
            "       💣\n\n\n\n\n\n\n             <:fucky:875070543915790366>",
            "       ⤵️\n       💣\n\n\n\n\n             <:fucky:875070543915790366>",
            "        \n       ⤵️\n       💣\n\n\n\n             <:fucky:875070543915790366>",
            "        \n        \n       ⤵️\n       💣\n\n\n             <:fucky:875070543915790366>",
            "        \n        \n        \n       ⤵️\n       💣\n\n             <:fucky:875070543915790366>",
            "        \n        \n        \n        \n       ⤵️\n       💣\n             <:fucky:875070543915790366>",
            "        \n        \n        \n        \n        \n       ⤵️\n       💣      <:fucky:875070543915790366>",
            "        \n        \n        \n        \n        \n        \n       💥      🔥",
        };

        for (var i = 0; i < frames.Length; i++)
        {
            var content = $"**TROLOLOL!** You rolled a **7**!\n{targetFlag} `incoming...`\n```{frames[i]}```";
            await command.ModifyOriginalResponseAsync(props => props.Content = content);
            await Task.Delay(800, cancellationToken);
        }

        await command.ModifyOriginalResponseAsync(props =>
            props.Content = $"**TROLOLOL!** You rolled a **7**!\n{targetFlag} :boom: **{targetFlag} got nuked!**");
    }
}
