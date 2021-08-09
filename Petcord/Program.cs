using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Net;
using Microsoft.Extensions.DependencyInjection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using static Petcord.Functions;

namespace Petcord
{
    class Program
    {
        private readonly ConfigFile _config;
        private readonly SheetsService _sheetService;

        private static void Main()
            => new Program().RunAsync().GetAwaiter().GetResult();

        public Program()
        {
            if (!File.Exists("config.json"))
            {
                Console.WriteLine("No config file found, exiting..");
                Environment.Exit(0);
            }
            _config = JsonSerializer.Deserialize<ConfigFile>(File.ReadAllText("config2.json"));

            // let garbage collector dispose of stream reader
            using var stream = new FileStream(_config.SheetsCredentialsFile, FileMode.Open, FileAccess.Read);
            var credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);

            _sheetService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _config.ApplicationName
            });
        }

        private async Task RunAsync()
        {
            // dependency injection
            await using var services = ConfigureServices();
            var client = services.GetRequiredService<DiscordSocketClient>();
            try
            {
                await client.LoginAsync(TokenType.Bot, _config.BotToken);
            }
            catch (HttpException httpEx)
            {
                Console.WriteLine($"Connection failed, reason: {httpEx.Reason}");
                return;
            }

            client.Ready += async () => await client.SetGameAsync(".Help ❓❔", null, ActivityType.Listening);
            client.Log += LogAsync;

            await client.StartAsync();
            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            // wait indefinitely while the app is running
            await Task.Delay(Timeout.Infinite);
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(_config)
                .AddSingleton(_sheetService)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    DefaultRunMode = RunMode.Async,
                    CaseSensitiveCommands = false
                }))
                .AddSingleton<CommandHandler>()
                .BuildServiceProvider();
        }

        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}
