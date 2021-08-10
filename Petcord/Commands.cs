using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using static Petcord.Functions;

namespace Petcord
{
    //has to be public and inherit ModuleBase<SocketCommandContext> to be discovered by .AddModulesAsync()
    public class Commands : ModuleBase<SocketCommandContext>
    {
        //dependency injection will set these
        public ConfigFile Config { get; set; }
        public SheetsService SheetService { get; set; }

        [Command("help")]
        public async Task Help([Remainder] string _ = "")
        {
            await Context.Channel.TriggerTypingAsync();
            var embed = new EmbedBuilder();
            embed.WithAuthor("Help", "https://i.imgur.com/PVyDsyp.png")
                .WithDescription("Hi, I'm a Discord bot for using the [Petcord Google Sheet](https://docs.google.com/spreadsheets/u/8/d/e/2PACX-1vQnNBDpAt7M7jcDyrgvKP3DoJ80pySN4tmehMG4xjVBsJoyQV_wyIpBSrfdr5Lgb4RT3vHRZsZRQhLI/pubhtml#)." +
                                 "\nYou can see the list of available commands below:\n\n" +
                                 "**.Pets**\nList the specified player's pet statistics\n\n" +
                                 "**.Top25**\nPrints the current top 25 pet hunters\n⠀")
                .WithFooter("Made By Meow 🐈/Ox#0254", "https://i.imgur.com/NQPxaFO.png")
                .WithThumbnailUrl("https://i.imgur.com/jAyyowq.png")
                .WithColor(RandomDiscordColor());

            await ReplyAsync(embed: embed.Build());
        }

        [Command("pets")]
        [Alias("petstats", "pet stats")]
        public async Task Pets()
        {
            await Context.Channel.TriggerTypingAsync();
            var embed = new EmbedBuilder();
            embed.WithAuthor("Pets Command", "https://i.imgur.com/PVyDsyp.png")
                .WithDescription("Lists the specified player's pet statistics.\n**Usage:**\n`.Pets <rsn>`")
                .AddField("**Example Usage**", ".Pets Velek")
                .WithThumbnailUrl("https://i.imgur.com/jAyyowq.png")
                .WithColor(Discord.Color.Blue);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("pets")]
        public async Task Pets([Remainder] string rsn)
        {
            try
            {
                await Context.Channel.TriggerTypingAsync();

                if (rsn.Length > 12 || !IsAlphaNum(rsn))
                {
                    await ReplyAsync(embed: ErrorEmbed("Error", $"The player name `{rsn}` is not valid"));
                    return;
                }

                var statsResponse = await SheetService.Spreadsheets.Values.Get(Config.SpreadsheetId, $"'Pet Hiscores'!{Config.PetHiscoresRange}").ExecuteAsync();
                var playerStats = statsResponse.Values.FirstOrDefault(x => ((string)x[2]).Equals(rsn, StringComparison.InvariantCultureIgnoreCase));

                var petsResponse = await SheetService.Spreadsheets.Values.Get(Config.SpreadsheetId, $"'Players Pets'!{Config.PlayersPetsStartCell}:{Config.PlayersPetsEndColumn}").ExecuteAsync();
                var playerPets = petsResponse.Values.FirstOrDefault(x => ((string)x[0]).Equals(rsn, StringComparison.InvariantCultureIgnoreCase));

                if (playerStats == null || playerPets == null)
                {
                    await ReplyAsync(embed: ErrorEmbed("Error", "Only Pet Hunters with 20+ pets are eligible for the hiscores.\n" +
                                                                "See <#801585244481388564> for more info"));
                    return;
                }

                var emotes = new List<string>();

                for (var i = 1; i <= Config.TotalPetCount; i++)
                    emotes.Add((string) playerPets[i] == "TRUE" ? Config.PetEmotes[i - 1] : Config.DisabledPetEmotes[i - 1]);

                var rank = (string)playerStats[1];
                var name = (string)playerStats[2];
                var pets = (string)playerStats[3];
                var hours = Convert.ToInt32(playerStats[4]);
                var progress = (string)playerStats[5];
                var legacy = (string) playerStats[0];

                var embed = new EmbedBuilder();
                embed.WithTitle($"**__{name.Replace("_", "\\_")}__'s Pets**")
                    .WithDescription($"**Rank:** {rank}\n" +
                                     (legacy != "-" ? $"**Legacy Rank:** {legacy}\n" : "") +
                                     $"**Pets:** {pets}\n" +
                                     $"**Hours:** {string.Format(NumberStringFormat, "{0:#,##0}", hours)}\n" +
                                     $"**Progress:** {progress}\n\n" +
                                     $"{string.Join(" ", emotes)}")
                    .WithColor(RandomDiscordColor())
                    .WithThumbnailUrl("https://i.imgur.com/LrrbHpD.png");
                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                ReportError(e, Context);
                try
                {
                    await ReplyAsync(embed: ErrorEmbed("Unexpected Error", $"Unexpected error experienced:\n{e.Message}\n\nMaybe this'll pass on its own\nI have reported this error in more detail to my master."));
                }
                catch
                {
                    // ignored
                }
            }
        }

        [Command("top25")]
        [Alias("top 25")]
        public async Task Top25()
        {
            try
            {
                await Context.Channel.TriggerTypingAsync();

                var response = await SheetService.Spreadsheets.Values.Get(Config.SpreadsheetId, $"'Pet Hiscores'!{Config.Top25Range}").ExecuteAsync();

                var ranks = response.Values.Select(x => x[0] + ".");
                var names = response.Values.Select(x => (string)x[1]);
                var petCounts = response.Values.Select(x => (string)x[2]);
                var hours = response.Values.Select(x => string.Format(NumberStringFormat, "{0:#,##0}", Convert.ToInt32(x[3])));
                var progresses = response.Values.Select(x => (string)x[4]);

                var rankLength = ranks.OrderByDescending(x => x.Length).FirstOrDefault().Length;
                var nameLength = names.OrderByDescending(x => x.Length).FirstOrDefault().Length;
                var petCountLength = petCounts.OrderByDescending(x => x.Length).FirstOrDefault().Length;
                var hoursLength = hours.OrderByDescending(x => x.Length).FirstOrDefault().Length;

                var table = $"```\n{"Rank".PadRight(rankLength)} {"Name".PadRight(nameLength)} {"Pets".PadRight(petCountLength)} {"Hours".PadRight(hoursLength)}  Progress\n";

                for (var i = 0; i < ranks.Count(); i++)
                    table += $"{ranks.ElementAt(i).PadRight(rankLength + NegativeToZero(4 - rankLength))} {names.ElementAt(i).PadRight(nameLength + NegativeToZero(4 - nameLength))} {petCounts.ElementAt(i).PadRight(petCountLength + NegativeToZero(4 - petCountLength))} {hours.ElementAt(i).PadRight(hoursLength + NegativeToZero(5 - hoursLength))}  {progresses.ElementAt(i)}\n";

                table += "```";

                var embed = new EmbedBuilder();
                embed.WithAuthor("Top 25 Pet Hunters", "https://i.imgur.com/LrrbHpD.png")
                    .WithDescription(table)
                    .WithColor(RandomDiscordColor());
                await ReplyAsync(embed: embed.Build());

            }
            catch (Exception e)
            {
                ReportError(e, Context);
                try
                {
                    await ReplyAsync(embed: ErrorEmbed("Unexpected Error", $"Unexpected error experienced:\n{e.Message}\n\nMaybe this'll pass on its own\nI have reported this error in more detail to my master."));
                }
                catch
                {
                    // ignored
                }
            }
        }

        [Command("add")]
        [Alias("update")]
        [RequireAdminRole]
        public async Task AddPlayer()
        {
            try
            {
                await Context.Channel.TriggerTypingAsync();

                var embed = new EmbedBuilder();
                embed.WithAuthor("Update/Add Command", "https://i.imgur.com/PVyDsyp.png")
                    .WithDescription("Updates the specified player's pets, or adds a new player with the specified pets.\n**Usage:**\n`.Add/Update <rsn> <emote(s)>`")
                    .AddField("**Example Usages**", ".Add Meoow <:penance:802580935513341953> <:nibbler:802580028147630120> <:bloodhound:802580935731445792>\n" +
                                                    ".Update Meows Alt <:snakeling:802580028155887666><:prime:802580028163358721><:mole:802580028537569340><:hydra:808878253422280705>")
                    .WithThumbnailUrl("https://i.imgur.com/jAyyowq.png")
                    .WithColor(Discord.Color.Blue);

                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                ReportError(e, Context);
                try
                {
                    await ReplyAsync(embed: ErrorEmbed("Unexpected Error", $"Unexpected error experienced:\n{e.Message}\n\nMaybe this'll pass on its own\nI have reported this error in more detail to my master."));
                }
                catch
                {
                    // ignored
                }
            }
        }

        [Command("add")]
        [Alias("update", "add/update")]
        [RequireAdminRole]
        public async Task AddPlayer([Remainder]string input)
        {
            try
            {
                await Context.Channel.TriggerTypingAsync();

                var emoteStartIndex = input.IndexOf('<');
                if (emoteStartIndex < 1)
                {
                    await ReplyAsync(embed: ErrorEmbed("Error", "I wasn't quite able to understand that.", "Example Usages", ".Add Meoow <:penance:802580935513341953> <:nibbler:802580028147630120> <:bloodhound:802580935731445792>\n" +
                                                                                                                             ".Update Meows Alt <:snakeling:802580028155887666><:prime:802580028163358721><:mole:802580028537569340><:hydra:808878253422280705>"));
                    return;
                }

                var rsn = input[..emoteStartIndex].Trim();
                if (rsn.Length > 12 || !IsAlphaNum(rsn))
                {
                    await ReplyAsync(embed: ErrorEmbed("Error", $"The player name `{rsn}` is not valid"));
                    return;
                }

                var matches = Regex.Matches(input[emoteStartIndex..], @"(<:\w+?:\d+?>)");

                if (matches.Count == 0)
                {
                    await ReplyAsync(embed: ErrorEmbed("Error", "I wasn't quite able to understand that.", "Example Usages", ".Add Meoow <:penance:802580935513341953> <:nibbler:802580028147630120> <:bloodhound:802580935731445792>\n" +
                                                                                                                             ".Update Meows Alt <:snakeling:802580028155887666><:prime:802580028163358721><:mole:802580028537569340><:hydra:808878253422280705>"));
                    return;
                }

                var emotes = new List<string>();
                foreach (Match match in matches)
                    emotes.Add(match.Value);

                emotes = emotes.Distinct().Where(emote => Config.PetEmotes2.Contains(emote) || Config.PetEmotes.Contains(emote)).ToList();

                if (emotes.Count == 0)
                {
                    await ReplyAsync(embed: ErrorEmbed("Error", "Hmm.. I didn't recongnize any of those emotes.\n" +
                                                                "Did you use the correct emotes?", "Example Usages", ".Add Meoow <:penance:802580935513341953> <:nibbler:802580028147630120> <:bloodhound:802580935731445792>\n" +
                                                                                                                     ".Update Meows Alt <:snakeling:802580028155887666><:prime:802580028163358721><:mole:802580028537569340><:hydra:808878253422280705>"));
                    return;
                }

                var embed = new EmbedBuilder();

                lock (Locker)
                {
                    var playerCountResponse = SheetService.Spreadsheets.Values.Get(Config.SpreadsheetId, $"'Players Pets'!{Config.PlayerCountRange}").Execute();

                    var column = playerCountResponse.Values.FirstOrDefault(x => (x.Count > 0 ? (string)x[0] : "").Equals(rsn, StringComparison.InvariantCultureIgnoreCase));

                    var updateRange = new List<object> { rsn };

                    if (column == null) //add new player
                    {
                        var rowNumber = playerCountResponse.Values.Count + 2;
                        updateRange.AddRange(Config.PetEmotes.Select((t, i) => emotes.Contains(t) || emotes.Contains(Config.PetEmotes2[i]) ? "TRUE" : "FALSE"));

                        var updateRequest = SheetService.Spreadsheets.Values.Update(new ValueRange { Values = new IList<object>[] { updateRange } }, Config.SpreadsheetId, $"'Players Pets'!A{rowNumber}:{Config.PlayersPetsEndColumn}{rowNumber}");
                        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                        updateRequest.Execute();

                        embed.WithTitle("**Added a player**")
                            .WithUrl($"https://docs.google.com/spreadsheets/d/{Config.SpreadsheetId}/edit#gid=1603667422&range=A{rowNumber}")
                            .WithDescription($"**Player:** {rsn}\n" +
                                             $"**Row:** #{rowNumber}\n" +
                                             $"**Pets:** {string.Join(" ", emotes)} (Count: {emotes.Count})")
                            .WithThumbnailUrl("https://i.imgur.com/5HTIiBD.png")
                            .WithColor(Colors.Success);
                    }
                    else
                    {
                        var rowNumber = playerCountResponse.Values.IndexOf(column) + 2;
                        updateRange.AddRange(Config.PetEmotes.Select((t, i) => emotes.Contains(t) || emotes.Contains(Config.PetEmotes2[i]) ? "TRUE" : null));

                        var updateRequest = SheetService.Spreadsheets.Values.Update(new ValueRange { Values = new IList<object>[] { updateRange } }, Config.SpreadsheetId, $"'Players Pets'!A{rowNumber}:{Config.PlayersPetsEndColumn}{rowNumber}");
                        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                        updateRequest.Execute();

                        embed.WithTitle("**Updated a player**")
                            .WithUrl($"https://docs.google.com/spreadsheets/d/{Config.SpreadsheetId}/edit#gid=1603667422&range=A{rowNumber}")
                            .WithDescription($"**Player:** {rsn}\n" +
                                             $"**Row:** #{rowNumber}\n" +
                                             $"**Added Pets:** {string.Join(" ", emotes)} (Count: {emotes.Count})")
                            .WithThumbnailUrl("https://i.imgur.com/5HTIiBD.png")
                            .WithColor(Colors.Success);
                    }
                }

                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                ReportError(e, Context);
                try
                {
                    await ReplyAsync(embed: ErrorEmbed("Unexpected Error", $"Unexpected error experienced:\n{e.Message}\n\nMaybe this'll pass on its own\nI have reported this error in more detail to my master."));
                }
                catch
                {
                    // ignored
                }
            }
        }

        [Command("remove")]
        [RequireAdminRole]
        public async Task RemovePlayer()
        {
            try
            {
                await Context.Channel.TriggerTypingAsync();

                var embed = new EmbedBuilder();
                embed.WithAuthor("Update/Add Command", "https://i.imgur.com/PVyDsyp.png")
                    .WithDescription("Updates the specified player's pets, or adds a new player with the specified pets.\n**Usage:**\n`.Add/Update <rsn> <emote(s)>`")
                    .AddField("**Example Usages**", ".Add Meoow <:penance:802580935513341953> <:nibbler:802580028147630120> <:bloodhound:802580935731445792>\n" +
                                                    ".Update Meows Alt <:snakeling:802580028155887666><:prime:802580028163358721><:mole:802580028537569340><:hydra:808878253422280705>")
                    .WithThumbnailUrl("https://i.imgur.com/jAyyowq.png")
                    .WithColor(Discord.Color.Blue);

                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                ReportError(e, Context);
                try
                {
                    await ReplyAsync(embed: ErrorEmbed("Unexpected Error", $"Unexpected error experienced:\n{e.Message}\n\nMaybe this'll pass on its own\nI have reported this error in more detail to my master."));
                }
                catch
                {
                    // ignored
                }
            }
        }

        [Command("remove")]
        [RequireAdminRole]
        public async Task RemovePlayer([Remainder]string input)
        {
            try
            {
                await Context.Channel.TriggerTypingAsync();

                var emoteStartIndex = input.IndexOf('<');
                if (emoteStartIndex < 1)
                {
                    await ReplyAsync(embed: ErrorEmbed("Error", "I wasn't quite able to understand that.", "Example Usages", ".Remove Meoow <:penance:802580935513341953> <:nibbler:802580028147630120> <:bloodhound:802580935731445792>\n" +
                                                                                                                             ".Remove Meows Alt <:snakeling:802580028155887666><:prime:802580028163358721><:mole:802580028537569340><:hydra:808878253422280705>"));
                    return;
                }

                var rsn = input[..emoteStartIndex].Trim();
                if (rsn.Length > 12 || !IsAlphaNum(rsn))
                {
                    await ReplyAsync(embed: ErrorEmbed("Error", $"The player name `{rsn}` is not valid"));
                    return;
                }

                var matches = Regex.Matches(input[emoteStartIndex..], @"(<:\w+?:\d+?>)");

                if (matches.Count == 0)
                {
                    await ReplyAsync(embed: ErrorEmbed("Error", "I wasn't quite able to understand that.", "Example Usages", ".Remove Meoow <:penance:802580935513341953> <:nibbler:802580028147630120> <:bloodhound:802580935731445792>\n" +
                                                                                                                             ".Remove Meows Alt <:snakeling:802580028155887666><:prime:802580028163358721><:mole:802580028537569340><:hydra:808878253422280705>"));
                    return;
                }

                var emotes = new List<string>();
                foreach (Match match in matches)
                    emotes.Add(match.Value);

                emotes = emotes.Distinct().Where(emote => Config.PetEmotes2.Contains(emote) || Config.PetEmotes.Contains(emote)).ToList();

                if (emotes.Count == 0)
                {
                    await ReplyAsync(embed: ErrorEmbed("Error", "Hmm.. I didn't recongnize any of those emotes.\n" +
                                                                "Did you use the correct emotes?", "Example Usages", ".Remove Meoow <:penance:802580935513341953> <:nibbler:802580028147630120> <:bloodhound:802580935731445792>\n" +
                                                                                                                     ".Remove Meows Alt <:snakeling:802580028155887666><:prime:802580028163358721><:mole:802580028537569340><:hydra:808878253422280705>"));
                    return;
                }

                var embed = new EmbedBuilder();

                lock (Locker)
                {
                    var playerCountResponse = SheetService.Spreadsheets.Values.Get(Config.SpreadsheetId, $"'Players Pets'!{Config.PlayerCountRange}").Execute();

                    var column = playerCountResponse.Values.FirstOrDefault(x => (x.Count > 0 ? (string)x[0] : "").Equals(rsn, StringComparison.InvariantCultureIgnoreCase));

                    var updateRange = new List<object> { rsn };

                    if (column != null)
                    {
                        var rowNumber = playerCountResponse.Values.IndexOf(column) + 2;
                        updateRange.AddRange(Config.PetEmotes.Select((t, i) => emotes.Contains(t) || emotes.Contains(Config.PetEmotes2[i]) ? "FALSE" : null));

                        var updateRequest = SheetService.Spreadsheets.Values.Update(new ValueRange { Values = new IList<object>[] { updateRange } }, Config.SpreadsheetId, $"'Players Pets'!A{rowNumber}:{Config.PlayersPetsEndColumn}{rowNumber}");
                        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                        updateRequest.Execute();

                        embed.WithTitle("**Updated a player**")
                            .WithUrl($"https://docs.google.com/spreadsheets/d/{Config.SpreadsheetId}/edit#gid=1603667422&range=A{rowNumber}")
                            .WithDescription($"**Player:** {rsn}\n" +
                                             $"**Row:** #{rowNumber}\n" +
                                             $"**Removed Pets:** {string.Join(" ", emotes)} (Count: {emotes.Count})")
                            .WithThumbnailUrl("https://i.imgur.com/5HTIiBD.png")
                            .WithColor(Colors.Success);
                    }
                    else
                    {
                        embed = ErrorEmbed("Error", $"No player by the name of `{rsn}` found.").ToEmbedBuilder();
                    }
                }

                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                ReportError(e, Context);
                try
                {
                    await ReplyAsync(embed: ErrorEmbed("Unexpected Error", $"Unexpected error experienced:\n{e.Message}\n\nMaybe this'll pass on its own\nI have reported this error in more detail to my master."));
                }
                catch
                {
                    // ignored
                }
            }
        }


        public async void ReportError(Exception e, SocketCommandContext context, string extraMsg = null)
        {
            try
            {
                var channel = await context.Guild.GetUser(Config.MaintainerId).CreateDMChannelAsync();
                var embed = new EmbedBuilder();
                embed.WithTitle("Error Report")
                    .WithDescription($"Command: \"{context.Message}\"\nUser: {context.User.Username}#{context.User.DiscriminatorValue}\nGuild: {context.Guild.Name} ({context.Guild.Id})\nChannel: {context.Channel.Name} ({context.Channel.Id})")
                    .AddField("Message", e.Message)
                    .AddField("Source", e.Source)
                    .AddField("Target Site", e.TargetSite)
                    .AddField("Stack Trace", e.StackTrace);
                if (extraMsg != null)
                    embed.AddField("Extra MSG", extraMsg);
                await channel.SendMessageAsync(string.Empty, embed: embed.Build());
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}

