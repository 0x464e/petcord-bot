using Discord.WebSocket;
using System;
using Discord;
using Discord.Commands;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using static Petcord.Functions;

namespace Petcord
{
    class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly ConfigFile _config;
        private readonly IServiceProvider _services;

        //constructor
        public CommandHandler(IServiceProvider services)
        {
            _commandService = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _config = services.GetRequiredService<ConfigFile>();
            _services = services;

            //hook message received event to process for possible commands
            _client.MessageReceived += MessageReceivedAsync;
            _commandService.CommandExecuted += CommandExecutedAsync;
        }

        //initializes all commands
        public async Task InitializeAsync()
        {
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task MessageReceivedAsync(SocketMessage s)
        {
            // ignore system messages and messages from bot/webhook users
            if (!(s is SocketUserMessage { Source: MessageSource.User } message)) return;

            var context = new SocketCommandContext(_client, message);
            try
            {
                //maybe forgot to make the bot private or something,
                //fail safe so it can't be used in other guilds
                if (context.Guild.Id != _config.GuildId) return;
            }
            catch
            {
                return;
            }

            //only work in DMs for the bot maintainer
            if (context.IsPrivate && context.User.Id != _config.MaintainerId) return;

            var argPos = 0;
            if (message.HasCharPrefix('.', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                await _commandService.ExecuteAsync(context, argPos, _services);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> commandInfo, ICommandContext context, IResult result)
        {
            // message had the prefix, but there was no command
            // these are of no interest
            if (!commandInfo.IsSpecified) return;

            // add and update commands have the custom precondition RequireAdminRole
            if (result.Error == CommandError.UnmetPrecondition && (commandInfo.Value.Name == "add" || commandInfo.Value.Name == "update"))
                await context.Channel.SendMessageAsync(embed: ErrorEmbed("Error", "This command is locked to admins only."));
            else if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(embed: ErrorEmbed("Error", $"Unexpected error occurred:\n{result.ErrorReason}\n\nI have reported this error to my master."));
        }
    }
}
