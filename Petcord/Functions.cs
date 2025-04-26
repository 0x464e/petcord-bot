using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Discord;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Petcord
{
    //some static helper functions and variables
    public static class Functions
    {
        private static Random Random = new();
        public static readonly IFormatProvider NumberStringFormat = new CultureInfo("en-US");

        //static object for all command instances
        //used to lock access at certain times to not make concurrently running
        //commands break each other's functionality
        public static readonly object Locker = new();

        public static bool IsAlphaNum(string str)
        {
            return str.Where(c => !char.IsLetter(c) && !char.IsNumber(c)).All(c => c == ' ' || c == '_' || c == '-');
        }

        public static Embed ErrorEmbed(string ErrorName, string ErrorMessage, string Field2 = null, string Field25 = null, string FooterText = null, string FooterIconURL = null)
        {
            var builder = new EmbedBuilder();
            builder
                .WithTitle($"**{ErrorName}**")
                .WithDescription(ErrorMessage)
                .WithThumbnailUrl("http://i.imgur.com/m2WdsOq.png")
                .WithColor(Colors.Error);

            if (Field2 != null || Field25 != null)
                builder.AddField($"**{Field2}**", Field25);
            if (FooterText != null)
                builder.WithFooter(x =>
                {
                    x.Text = FooterText;
                    x.IconUrl = FooterIconURL;
                });

            return builder.Build();
        }

        public static int NegativeToZero(int num)
        {
            return num < 0 ? 0 : num;
        }

        public static Color RandomDiscordColor()
        {
            return new Color(Random.Next(256), Random.Next(256), Random.Next(256));
        }

        internal abstract class Colors
        {
            public static readonly Color Error = new Color(0xff0000);
            public static readonly Color Success = new Color(0x00fc08);
        }

        public class EmoteJsonConverter : JsonConverter<List<Emote>>
        {
            public override List<Emote> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var emotes = new List<Emote>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return emotes;

                    if (reader.TokenType == JsonTokenType.String)
                        emotes.Add(Emote.Parse(reader.GetString()));
                }
                
                throw new JsonException("Unexpected end of JSON input");
            }

            public override void Write(Utf8JsonWriter writer, List<Emote> value, JsonSerializerOptions options) => throw new NotImplementedException();
        }

        public class ConfigFile
        {
            public string ApplicationName { get; init; }
            public string SpreadsheetId { get; init; }
            public string SheetsCredentialsFile { get; init; }
            public string BotToken { get; init; }
            
            [JsonConverter(typeof(EmoteJsonConverter))]
            public List<Emote> PetEmotes { get; init; }
            [JsonConverter(typeof(EmoteJsonConverter))]
            public List<Emote> PetEmotes2 { get; init; }
            [JsonConverter(typeof(EmoteJsonConverter))]
            public List<Emote> DisabledPetEmotes { get; init; }
            
            public string PetHiscoresRange { get; init; }
            public string PlayersPetsStartCell { get; init; }
            public string PlayersPetsEndColumn { get; init; }
            public string Top25Range { get; init; }
            public string PlayerCountRange { get; init; }
            public int TotalPetCount { get; init; }
            public ulong MaintainerId { get; init; }
            public ulong AdminRoleId { get; init; }
            public ulong GuildId { get; init; }
            public ulong LeaderboardChannelId { get; init; }
        }

    }
}
