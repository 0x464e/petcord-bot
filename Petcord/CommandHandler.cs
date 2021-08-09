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
            {
                try
                {
                    var result = await _commandService.ExecuteAsync(context, argPos, _services);
                    if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                        await context.Channel.SendMessageAsync($"Unexpected Error:\n{result.ErrorReason}\n\nI have reported this error to my master.");
                }
                catch (Exception e)
                {
                    ReportError(e, context);
                }
            }
        }


        public async void ReportError(Exception e, SocketCommandContext context = null, string extraMsg = null)
        {
            try
            {
                var channel = await _client.GetUser(_config.MaintainerId).CreateDMChannelAsync();
                var embed = new EmbedBuilder();
                embed.WithTitle("Error Report");
                if (context != null)
                    embed.WithDescription($"Command: \"{context.Message}\"\nUser: {context.User.Username}#{context.User.DiscriminatorValue}\nGuild: {context.Guild.Name} ({context.Guild.Id})\nChannel: {context.Channel.Name} ({context.Channel.Id})");
                embed.AddField("Message", e.Message)
                    .AddField("Source", e.Source)
                    .AddField("Target Site", e.TargetSite)
                    .AddField("Stack Trace", e.StackTrace);
                if (extraMsg != null)
                    embed.AddField("Extra MSG", extraMsg);
                await channel.SendMessageAsync(string.Empty, embed: embed.Build());
            }
            catch
            {
                // ignored
            }
        }
    }
}
