using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using KeySeller.Business;
using KeySeller.StaticVars;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using Newtonsoft.Json;
using QBitNinja.Client;

namespace KeySeller
{
    // Invite link: https://discord.com/oauth2/authorize?client_id=736541614440448020&permissions=8&scope=bot
    // Color raw value (uint): https://www.shodor.org/stella2java/rgbint.html
    // Bitcoin Address: 1LMod1zP227qSBZsd3caX8dJ8ahNGceeBE
    public class Program
    {
        static void Main(string[] args)
            => new Program().StartAsync().GetAwaiter().GetResult();

        private readonly DiscordColor _discordColor = new DiscordColor();
        private DiscordSocketClient _client;
        private CommandHandler _handler;

        public async Task StartAsync()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Starting bot...");
            
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });

            _client.Log += Log;

            ConfigJson configJson;
            using (var r = new StreamReader("config.json"))
            {
                var json = await r.ReadToEndAsync();
                configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
            }

            await _client.LoginAsync(TokenType.Bot, configJson.Token);
            await _client.StartAsync();

            _handler = new CommandHandler(_client);

            while (_client.ConnectionState.ToString() != "Connected") { }
            await Task.Delay(650);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Bot connected!");

            // Change custom status message
            CustomStatus("your life problems.");
            // Checking BTC transaction confirmations
            var confirmTimer = new Timer(ConfirmCheck, null, 0, 1000 * 60 * 5); // Check every 5 minutes
            // Checking order validity
            var orderTimer = new Timer(OrderCheck, null, 0, 1000 * 60 * 60 * 24); // Check every 24 hours

            while (_client.ConnectionState.ToString() == "Connected")
            {
                var delay = Task.Delay(60000);
                Console.WriteLine($"\nClient latency: {_client.Latency}ms");
                await delay;
            }

            await Task.Delay(-1);
        }

        private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

        private async void CustomStatus(string status)
        {
            await _client.SetGameAsync(status,
                type: ActivityType.Listening);
        }

        private async void ConfirmCheck(object state)
        {
            var db = new MongoCRUD("GameSelling");
            var orders = db.LoadRecords<Order>("Orders").FindAll(x => x.TransactionId != null);

            foreach (var order in orders)
            {
                var client = new QBitNinjaClient("http://api.qbit.ninja/", Network.Main);
                var transaction =
                    await client.GetTransaction(
                        new uint256(order.TransactionId));
                order.Confirmations = transaction.Block.Confirmations;

                if (order.Confirmations >= 3 && !order.Confirmed)
                {
                    var customer = _client.GetUser(Convert.ToUInt64(order.Customer.UserId));
                    await _client.GetUser(299582273324449803).SendMessageAsync(
                        $"**ORDER CONFIRMED:**\n**Ordered by** `{customer.Username}#{customer.Discriminator}`\n**OrderId:** `{order.Id}`\n**Confirmations:** `{order.Confirmations}`");
                    order.Confirmed = true;
                }

                db.UpsertRecord("Orders", order.Id, order);
            }
        }

        private async void OrderCheck(object state)
        {
            var db = new MongoCRUD("GameSelling");
            var orders = db.LoadRecords<Order>("Orders").FindAll(x => x.TransactionId == null);

            foreach (var order in orders)
            {
                var embedBuilder = new EmbedBuilder
                {
                    Title = $"**Order Removal**\nOrder ID: `{order.Id}`",
                    Color = _discordColor.Orange
                };

                if (DateTime.UtcNow.Subtract(order.Date).Days >= 2)
                {
                    if (DateTime.UtcNow.Subtract(order.Date).Days < 3)
                    {
                        embedBuilder.Description = "**Your placed order will get deleted in 24 hours if you do not add a BTC transaction ID using the `confirm` command.**";
                        embedBuilder.Footer = new EmbedFooterBuilder
                        {
                            Text = "To see more details about this order, use the 'myorders' command (followed by your order id)."
                        };
                    }
                    else
                    {
                        // Set picked games' Sold property to false again
                        foreach (var game in order.Games)
                        {
                            game.Sold = false;
                            db.UpsertRecord("Games", game.Id, game);
                        }
                        
                        db.DeleteRecord<Order>("Orders", order.Id);

                        embedBuilder.Color = _discordColor.Red;
                        embedBuilder.Description =
                            "**Your placed order has been removed because you did not send the BTC in time (used the `confirm` command to add your BTC transaction ID to the order)**.\n\n*Why do we do this?*\n";
                        embedBuilder.Footer = new EmbedFooterBuilder
                        {
                            Text = "As long as a game is ordered by someone, but not yet paid, this game is not available for purchase for other people.\nAs we want to give everyone the same chance at buying the game they want, we do not support waiting more than 3 days for a sent payment."
                        };
                    }

                    await _client.GetUser(Convert.ToUInt64(order.Customer.UserId)).SendMessageAsync(embed: embedBuilder.Build());
                }
            }
        }
    }
}
