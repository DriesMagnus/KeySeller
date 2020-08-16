using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using KeySeller.Business;
using KeySeller.StaticVars;
using Game = KeySeller.Business.Game;

namespace KeySeller.Modules
{
    public class SellCommands : InteractiveBase
    {

        private readonly DiscordColor _discordColor = new DiscordColor();
        
        [Command("address")]
        public async Task Address()
        {
            await Context.User.SendMessageAsync("**Be wary of transaction fees (I do not receive the fee money)!\nYou need to send the right amount to my address but it might cost more to you because of these transaction fees!**\nSend the right amount to the following bitcoin address: `1LMod1zP227qSBZsd3caX8dJ8ahNGceeBE`");
        }

        [Command("info")]
        public async Task Info(int id)
        {
            var db = new MongoCRUD("GameSelling");
            var game = db.LoadRecordById<Game>("Games", id);
            
            if (Context.User is SocketGuildUser user && !user.GuildPermissions.Administrator)
            {
                game.Choice = null;
            }
            var choiceString = game.Choice == null ? "" : $"**Choice:** {game.Choice}\n";
            var embedBuilder = new EmbedBuilder { Title = $"Information of `{game.Name}`", Color = _discordColor.LightBlue };
            embedBuilder.WithDescription(
                $"**Id:** {game.Id}\n**Name:** {game.Name}\n**Price:** €{game.Price}\n**Sold:** {game.Sold}\n{choiceString}**VideoUrl:** {game.VideoUrl}");

            await Context.Channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        [Command("list")]
        public async Task List()
        {
            var db = new MongoCRUD("GameSelling");

            var authors = new List<Tuple<int, string, string, string>>();
            var games = db.LoadRecords<Game>("Games");

            if (games.Count > 0)
            {
                foreach (var game in games)
                {
                    var soldString = game.Sold ? "✔" : "✘";
                    var tuple = Tuple.Create(game.Id, game.Name, "€" + game.Price, soldString);
                    authors.Add(tuple);
                }

                string data = authors.ToStringTable(
                    new[] {"Id", "Name", "Price", "Sold"},
                    a => a.Item1, a => a.Item2, a => a.Item3, a => a.Item4);

                await Context.Channel.SendMessageAsync($"```{data}```");
            }
            else await Context.Channel.SendMessageAsync("There are no games in this database!");
        }

        [Command("listselling")]
        public async Task ListSelling()
        {
            var db = new MongoCRUD("GameSelling");

            var authors = new List<Tuple<int, string, string>>();
            var games = db.LoadRecords<Game>("Games").Where(x => x.Sold == false).ToList();

            if (games.Count > 0)
            {
                foreach (var game in games)
                {
                    var tuple = Tuple.Create(game.Id, game.Name, "€" + game.Price);
                    authors.Add(tuple);
                }

                string data = authors.ToStringTable(
                    new[] {"Id", "Name", "Price"},
                    a => a.Item1, a => a.Item2, a => a.Item3);

                await Context.Channel.SendMessageAsync($"```{data}```");
            }
            else await Context.Channel.SendMessageAsync("There are no games for sale at the moment!");
        }

        [Command("listsold")]
        public async Task ListSold()
        {
            var db = new MongoCRUD("GameSelling");

            var authors = new List<Tuple<int, string, string>>();
            var games = db.LoadRecords<Game>("Games").Where(x => x.Sold).ToList();

            if (games.Count > 0)
            {
                foreach (var game in games)
                {
                    var tuple = Tuple.Create(game.Id, game.Name, "€" + game.Price);
                    authors.Add(tuple);
                }

                string data = authors.ToStringTable(
                    new[] {"Id", "Name", "Price"},
                    a => a.Item1, a => a.Item2, a => a.Item3);

                await Context.Channel.SendMessageAsync($"```{data}```");
            }
            else await Context.Channel.SendMessageAsync("No games have been sold yet!");
        }

        [Command("add")]
        [Summary("bruh")]
        [RequireOwner]
        public async Task Add(string name, double price, bool sold, string videoUrl, string choiceName = null, int choicesLeft = 0)
        {
            var db = new MongoCRUD("GameSelling");
            var nextId = 1;
            if (db.LoadRecords<Game>("Games").Count > 0)
            {
                nextId = db.LoadRecords<Game>("Games").Last().Id + 1;
            }

            if (name.Contains('_'))
            {
                name = name.Replace('_', ' ');
            }

            if (choiceName != null && choiceName.Contains('_'))
            {
                choiceName = choiceName.Replace('_', ' ');
            }
            Console.WriteLine();
            var choice = choiceName == null ? null : new HumbleChoice { Name = choiceName, ChoicesLeft = choicesLeft };

            db.InsertRecord("Games", new Game
            {
                Id = nextId,
                Name = name,
                Price = price,
                Sold = sold,
                VideoUrl = videoUrl,
                Choice = choice
            });

            var choiceString = choice == null ? "" : $"**Choice:** {choice}\n";
            var embedBuilder = new EmbedBuilder { Title = "Add Log", Color = _discordColor.Green };
            embedBuilder.WithDescription(
                $"**Id:** {nextId}\n**Name:** {name}\n**Price:** €{price}\n**Sold:** {sold}\n{choiceString}**VideoUrl:** {videoUrl}");

            await Context.Channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        [Command("upsert", RunMode = RunMode.Async)]
        [Summary("nigger")]
        [RequireOwner]
        public async Task Upsert([Summary("The id of the game.")]int id)
        {
            var db = new MongoCRUD("GameSelling");
            var game = db.LoadRecordById<Game>("Games", id);

            await Context.Channel.SendMessageAsync("What property do you want to change?\n`Name` / `Price` / `Sold` / `ChoiceName` / `ChoicesLeft` / `VideoUrl`");
            var toUpsert = (await NextMessageAsync()).Content;
            await Context.Channel.SendMessageAsync("What is the new value?");
            var value = (await NextMessageAsync()).Content;
            
            try
            {
                switch (toUpsert.ToLower())
                {
                    case "name":
                        game.Name = value;
                        break;
                    case "price":
                        game.Price = Convert.ToDouble(value);
                        break;
                    case "sold":
                        game.Sold = Convert.ToBoolean(value);
                        break;
                    case "choicename":
                        game.Choice.Name = value;
                        break;
                    case "choicesleft":
                        game.Choice.ChoicesLeft = Convert.ToInt32(value);
                        break;
                    case "videourl":
                        game.VideoUrl = value;
                        break;
                    default:
                        throw new Exception($"Property `{toUpsert}` could not be found.");
                }

                db.UpsertRecord("Games", game.Id, game);

                var choiceString = game.Choice == null ? "" : $"**Choice:** {game.Choice}\n";
                var embedBuilder = new EmbedBuilder {Title = "Upsert Log", Color = _discordColor.Orange};
                embedBuilder.WithDescription(
                    $"**Id:** {game.Id}\n**Name:** {game.Name}\n**Price:** €{game.Price}\n**Sold:** {game.Sold}\n{choiceString}**VideoUrl:** {game.VideoUrl}");

                await Context.Channel.SendMessageAsync(embed: embedBuilder.Build());
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
            }
        }

        [Command("delete")]
        [RequireOwner]
        public async Task Delete(int id)
        {
            var db = new MongoCRUD("GameSelling");
            var game = db.LoadRecordById<Game>("Games", id);

            db.DeleteRecord<Game>("Games", id);

            var choiceString = game.Choice == null ? "" : $"**Choice:** {game.Choice}\n";
            var embedBuilder = new EmbedBuilder { Title = "Delete Log", Color = _discordColor.Red };
            embedBuilder.WithDescription(
                $"**Id:** {game.Id}\n**Name:** {game.Name}\n**Price:** €{game.Price}\n**Sold:** {game.Sold}\n{choiceString}**VideoUrl:** {game.VideoUrl}");

            await Context.Channel.SendMessageAsync(embed: embedBuilder.Build());
        }
    }
}