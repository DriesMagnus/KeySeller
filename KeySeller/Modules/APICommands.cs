using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using KeySeller.Business;
using KeySeller.StaticVars;
using NBitcoin;
using QBitNinja.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Schema;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Game = KeySeller.Business.Game;

namespace KeySeller.Modules
{
    public class APICommands : InteractiveBase
    {
        private readonly DiscordColor _discordColor = new DiscordColor();

        [Command("order")]
        [Summary("Shows all the information of an order.")]
        [RequireOwner]
        public async Task Order([Summary("ID of the order.")]int orderId)
        {
            var db = new MongoCRUD("GameSelling");
            try
            {
                var order = db.LoadRecordById<Order>("Orders", orderId);
                var customer = Context.Client.GetUser(order.Customer.UserId);

                if (order.TransactionId != null)
                {
                    var client = new QBitNinjaClient("http://api.qbit.ninja/", Network.Main);
                    var transaction =
                        await client.GetTransaction(
                            new uint256(order.TransactionId));
                    order.Confirmations = transaction.Block.Confirmations;
                }

                var embedBuilder = new EmbedBuilder
                {
                    Title = "Order details",
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = Context.Client.GetUser(order.Customer.UserId).GetAvatarUrl()
                    },
                    Color = _discordColor.LightBlue,
                    Description =
                        $"**Ordered by `{customer.Username}#{customer.Discriminator}`**\n**UserId:** `{customer.Id}`\n\n**OrderId:** {order.Id}\n**TransactionId:** `{order.TransactionId ?? "null"}`\n**Handled:** {order.Handled}\n**Confirmations:** {order.Confirmations}\n\n**Game(s) ordered:**\n"
                        + order.Games.Aggregate("",
                            (current, g) =>
                                current + $"**Id:** {g.Id}\n**Name:** {g.Name}\n**Price:** €{g.Price}\n\n") +
                        $"**Total price:** €{order.Games.Sum(x => x.Price)}"
                };

                db.UpsertRecord("Orders", order.Id, order);
                await ReplyAsync(embed: embedBuilder.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await ReplyAsync($"Order with order id `{orderId}` not found.");
            }
        }

        [Command("ordersof")]
        [Summary("Lists all the orders of a customer.")]
        [RequireOwner]
        public async Task OrdersOf([Summary("DiscordID of the customer.")]ulong customerId)
        {
            var db = new MongoCRUD("GameSelling");
            var orders = db.db.GetCollection<Order>("Orders")
                .Find(x => x.Customer.UserId == customerId).ToList();
            foreach (var order in orders)
            {
                await Order(order.Id);
            }
        }

        [Command("myorders")]
        public async Task MyOrders(params int[] ids)
        {
            if (Context.Channel.GetType() == typeof(SocketDMChannel))
            {
                var db = new MongoCRUD("GameSelling");
                var customer = Context.User;
                var orders = db.db.GetCollection<Order>("Orders")
                    .Find(x => x.Customer.UserId == customer.Id).ToList();

                if (orders.Count > 0)
                {
                    if (ids.Length > 0)
                    {
                        orders = ids.Select(id => db.LoadRecordById<Order>("Orders", id))
                            .Where(x => x.Customer.UserId == customer.Id).ToList();
                    }

                    foreach (var order in orders)
                    {
                        if (order.TransactionId != null)
                        {
                            var client = new QBitNinjaClient("http://api.qbit.ninja/", Network.Main);
                            var transaction =
                                await client.GetTransaction(
                                    new uint256(order.TransactionId));
                            order.Confirmations = transaction.Block.Confirmations;
                        }

                        var embedBuilder = new EmbedBuilder
                        {
                            Title = "Order details",
                            Author = new EmbedAuthorBuilder
                            {
                                IconUrl = Context.Client.GetUser(order.Customer.UserId).GetAvatarUrl()
                            },
                            Color = _discordColor.LightBlue,
                            Description =
                                $"**OrderId:** {order.Id}\n**TransactionId:** `{order.TransactionId ?? "null"}`\n**Handled:** {order.Handled}\n**Confirmations:** {order.Confirmations}\n\n**Game(s) ordered:**\n"
                                + order.Games.Aggregate("",
                                    (current, g) =>
                                        current + $"**Id:** {g.Id}\n**Name:** {g.Name}\n**Price:** €{g.Price}\n\n") +
                                $"**Total price:** €{order.Games.Sum(x => x.Price)}"
                        };

                        db.UpsertRecord("Orders", order.Id, order);
                        await ReplyAsync(embed: embedBuilder.Build());
                    }
                }
                else await ReplyAsync("You do not have any orders.");
            }
            else
            {
                await ReplyAsync("This command is only usable in the bot's DMs.");
            }
        }

        [Command("allorders")]
        public async Task AllOrders()
        {
            if (Context.Channel.GetType() == typeof(SocketDMChannel))
            {
                var db = new MongoCRUD("GameSelling");
                var authorsList = new List<List<Tuple<string, string, ulong>>>();
                var orders = db.LoadRecords<Order>("Orders");

                if (orders.Count > 0)
                {
                    var authors = new List<Tuple<string, string, ulong>>();

                    foreach (var order in orders.ToList())
                    {
                        var customer = Context.Client.GetUser(order.Customer.UserId);
                        //Console.WriteLine($"{customer.Username}#{customer.Discriminator}");
                        var tuple = Tuple.Create($"[{order.Id}]", "bruh"/*$"{customer.Username}#{customer.Discriminator}"*/, order.Customer.UserId);
                        authors.Add(tuple);

                        if (authors.Count > 9 || order == orders[^1])
                        {
                            authorsList.Add(authors.ToList());
                            authors.Clear();
                        }
                    }

                    var data = authorsList.Select(al => al.ToStringTable(new[] {"Id", "DiscordTag", "Discord ID"},
                        a => a.Item1, a => a.Item2, a => a.Item3)).ToList();

                    var pages = new List<object>();
                    foreach (var d in data)
                    {
                        pages.Add($"```ini\n{d}```");
                    }

                    var message = new PaginatedMessage
                    {
                        Pages = pages,
                        Options = {JumpDisplayOptions = JumpDisplayOptions.Never, DisplayInformationIcon = false}
                    };
                    await PagedReplyAsync(message);
                }
                else await Context.Channel.SendMessageAsync("There are no orders in this database!");
            }
            else
            {
                await ReplyAsync("This command is only usable in the bot's DMs.");
            }
        }

        [Command("buy", RunMode = RunMode.Async)]
        public async Task Buy(params int[] ids)
        {
            // TODO:
            // Ask me for confirmation before selling
            // Set picked games' Sold property to true
            if (Context.Channel.GetType() == typeof(SocketDMChannel))
            {
                if (ids.Length > 0)
                {
                    try
                    {
                        var db = new MongoCRUD("GameSelling");
                        var games = ids.Select(id => db.LoadRecordById<Game>("Games", id)).ToList();
                        var user = Context.User;

                        if (games.All(x => x.Sold == false))
                        {
                            var embedBuilder = new EmbedBuilder
                            {
                                Title = "Chosen game(s):",
                                Color = _discordColor.Green,
                                Description =
                                    games.Aggregate("",
                                        (current, g) =>
                                            current +
                                            $"**Id:** {g.Id}\n**Name:** {g.Name}\n**Price:** €{g.Price}\n\n") +
                                    $"**Total price:** €{games.Sum(x => x.Price)}"
                            };

                            await ReplyAsync(embed: embedBuilder.Build());
                            await ReplyAsync("Is this correct? (Type 'yes' or 'no')");
                            var confirmation = await NextMessageAsync(timeout: new TimeSpan(0, 1, 0));
                            if (confirmation != null && confirmation.Content.ToLower() == "yes")
                            {
                                var client = new WebClient();
                                var btc = client.DownloadString(
                                    $"https://blockchain.info/tobtc?currency=EUR&value={games.Sum(x => x.Price).ToString().Replace(',', '.')}");
                                await ReplyAsync(
                                    $"**Be wary of transaction fees (I do not receive the fee money)!\nYou need to send the right amount to my address but it might cost more to you because of these transaction fees!**\nSend `{btc}` BTC to address `1LMod1zP227qSBZsd3caX8dJ8ahNGceeBE`\nAfter sending the BTC, type `;confirm (transactionId)` using the ID of the BTC transaction.");

                                #region ObjCreation

                                var objDb = new MongoCRUD("GameSelling");
                                var nextId = 1;
                                if (objDb.LoadRecords<Order>("Orders").Count > 0)
                                {
                                    nextId = objDb.LoadRecords<Order>("Orders").Last().Id + 1;
                                }

                                // Order
                                var order = new Order
                                {
                                    Id = nextId,
                                    TransactionId = null,
                                    Handled = false,
                                    Confirmations = 0,
                                    Games = games,
                                    Date = DateTime.UtcNow
                                };
                                objDb.InsertRecord("Orders", order);

                                // Customer
                                var orders = objDb.db.GetCollection<Order>("Orders")
                                    .Find(x => x.Customer.UserId == user.Id).ToList();
                                var customer = orders.Count > 0 ? orders[0].Customer : new Customer {UserId = user.Id};

                                order.Customer = customer;
                                objDb.UpsertRecord("Orders", order.Id, order);

                                #endregion

                                #region DM

                                embedBuilder.Title = "Order details";
                                embedBuilder.Description =
                                    embedBuilder.Description.Insert(0,
                                        $"**Ordered by `{user.Username}#{user.Discriminator}`**\n**UserId:** `{user.Id}`\n**OrderId:** {order.Id}\n\n**Game(s) ordered:**\n");
                                await Context.Client.GetUser(299582273324449803)
                                    .SendMessageAsync(embed: embedBuilder.Build());

                                #endregion
                            }
                            else
                            {
                                if (confirmation == null)
                                {
                                    await ReplyAsync("You did not reply in time. Order canceled. Try again.");
                                }
                                else await ReplyAsync("Order canceled. Try again.");
                            }
                        }
                        else
                        {
                            var embedBuilder = new EmbedBuilder
                            {
                                Title = "The games listed below have already been sold!\nChoose another game.",
                                Color = _discordColor.Red,
                                Description = games.Where(g => g.Sold).Aggregate("",
                                    (current, g) =>
                                        current + $"**Id:** {g.Id}\n**Name:** {g.Name}\n**Price:** €{g.Price}\n\n")
                            };

                            await ReplyAsync(embed: embedBuilder.Build());
                        }
                    }
                    catch (Exception e)
                    {
                        if (e.Message == "Sequence contains no elements")
                        {
                            await ReplyAsync("One of the given IDs does not exist.");
                        }
                        else
                        {
                            await ReplyAsync(
                                "An unknown error occurred, please check your spelling.\nError message: " + e.Message);
                        }
                    }
                }
                else
                {
                    await ReplyAsync("You have to give at least one game id.");
                }
            }
            else
            {
                await ReplyAsync("This command is only usable in the bot's DMs.");
            }
        }

        [Command("confirm", RunMode = RunMode.Async)]
        public async Task Confirm(string transactionId)
        {
            if (Context.Channel.GetType() == typeof(SocketDMChannel))
            {
                try
                {
                    var client = new QBitNinjaClient("http://api.qbit.ninja/", Network.Main);
                    var transaction =
                        await client.GetTransaction(
                            new uint256(transactionId));
                    var db = new MongoCRUD("GameSelling");
                    var orders = db.db.GetCollection<Order>("Orders")
                        .Find(x => x.Customer.UserId == Context.User.Id && !x.Handled).ToList();
                    Order order = null;

                    if (orders.Count > 0)
                    {
                        if (orders.Count > 1)
                        {
                            await MyOrders(orders.Select(x => x.Id).ToArray());
                            await ReplyAsync(
                                "I see you have multiple unfinished orders (which are listed above). To which order do you want to either change/add the BTC transaction id?\n(Just type the order ID (number), f.a. `5`)");
                            try
                            {
                                var orderId = Convert.ToInt32((await NextMessageAsync()).Content);
                                order = db.LoadRecordById<Order>("Orders", orderId);
                            }
                            catch (Exception e)
                            {
                                if (e.Message == "Input string was not in a correct format." ||
                                    e.Message == "Sequence contains no elements")
                                {
                                    await ReplyAsync(
                                        "That is not a valid order ID. Action canceled. Try again using the `confirm` command.");
                                }
                                else
                                {
                                    await ReplyAsync("Something went wrong. Error message: " + e.Message);
                                }
                            }
                        }
                        else
                        {
                            order = db.LoadRecordById<Order>("Orders", orders[0].Id);
                        }

                        if (order != null)
                        {
                            await MyOrders(order.Id);
                            await ReplyAsync(
                                $"You want to change the transaction ID of the order above to\n`{transactionId}` which currently has `{transaction.Block.Confirmations}` confirmations.\nIs this correct? (Type 'yes' or 'no')");
                            var confirmation = await NextMessageAsync(timeout: new TimeSpan(0, 1, 0));
                            if (confirmation != null && confirmation.Content.ToLower() == "yes")
                            {
                                order.TransactionId = transactionId;
                                db.UpsertRecord("Orders", order.Id, order);
                                await MyOrders(order.Id);
                                await ReplyAsync(
                                    "Done! The transaction ID of the order above has been updated.\nYou can always change it again using the `confirm` command.");
                            }
                            else
                            {
                                if (confirmation == null)
                                {
                                    await ReplyAsync(
                                        "You did not reply in time. Action canceled. Try again using the `confirm` command.");
                                }
                                else await ReplyAsync("Action canceled. Try again using the `confirm` command.");
                            }
                        }
                    }
                    else
                    {
                        await ReplyAsync(
                            "You do not have any unhandled orders. You cannot change the transaction ID of an old order.");
                    }
                }
                catch (Exception e)
                {
                    if (e.Message == "Invalid Hex String")
                    {
                        await ReplyAsync("The transaction ID is not valid, are you sure you typed it correctly?");
                    }
                    else
                    {
                        await ReplyAsync("An unknown error occurred. Error message: " + e.Message);
                    }
                }
            }
            else
            {
                await ReplyAsync("This command is only usable in the bot's DMs.");
            }
        }

        [Command("cancel", RunMode = RunMode.Async)]
        public async Task Cancel()
        {
            var channel = Context.Channel;

            if (channel.GetType() == typeof(SocketDMChannel))
            {
                await ForceCancel(Context.User.Id);
                //try
                //{
                //    var db = new MongoCRUD("GameSelling");
                //    var orders = db.LoadRecords<Order>("Orders").FindAll(x => x.Customer.UserId == Context.User.Id);

                //    Order toCancel;
                //    if (orders.Count == 1)
                //    {
                //        toCancel = orders[0];
                //    }
                //    else
                //    {
                //        await Orders(Context.User.Id);
                //        await ReplyAsync("What order do you want to cancel (give the OrderID)?");
                //        var confirmation = await NextMessageAsync(timeout: new TimeSpan(0, 2, 0));

                //        if (confirmation != null)
                //        {
                //            toCancel = orders.Find(x => x.Id == Convert.ToInt32(confirmation.Content));
                //        }
                //        else
                //        {
                //            await ReplyAsync("You did not reply in time. Action canceled. Try again using the `cancel` command.");
                //            return;
                //        }
                //    }

                //    await Order(toCancel.Id);
                //    await ReplyAsync("Are you sure you want to cancel the order above? (Type 'yes' or 'no')");
                //    var confirm = await NextMessageAsync(timeout: new TimeSpan(0, 2, 0));
                //    if (confirm != null && confirm.Content.ToLower() == "yes")
                //    {
                //        db.DeleteRecord<Order>("Orders", toCancel.Id);
                //        await ReplyAsync($"Purchase order with orderID `{toCancel.Id}` successfully canceled!");
                //    }
                //    else
                //    {
                //        if (confirm == null)
                //        {
                //            await ReplyAsync(
                //                "You did not reply in time. Action canceled. Try again using the `cancel` command.");
                //        }
                //        else await ReplyAsync("Action canceled. Try again using the `cancel` command.");
                //    }
                //}
                //catch (Exception e)
                //{
                //    if (e.Message == "Input string was not in a correct format.")
                //    {
                //        await ReplyAsync("That is not a number. Cancellation stopped. Try again.");
                //    }
                //    else
                //    {
                //        await ReplyAsync("An error occurred. Error message: " + e.Message);
                //    }
                //}
            }
            else
            {
                await ReplyAsync("This command is only usable in the bot's DMs.");
            }
        }

        [Command("forcecancel", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task ForceCancel(ulong userId)
        {
            var channel = Context.Channel;

            if (channel.GetType() == typeof(SocketDMChannel))
            {
                try
                {
                    var db = new MongoCRUD("GameSelling");
                    var orders = db.LoadRecords<Order>("Orders").FindAll(x => x.Customer.UserId == userId);

                    Order toCancel;
                    if (orders.Count == 1)
                    {
                        toCancel = orders[0];
                    }
                    else
                    {
                        await OrdersOf(Context.User.Id);
                        await ReplyAsync("What order do you want to cancel (give the OrderID)?");
                        var confirmation = await NextMessageAsync(timeout: new TimeSpan(0, 2, 0));

                        if (confirmation != null)
                        {
                            toCancel = orders.Find(x => x.Id == Convert.ToInt32(confirmation.Content));
                        }
                        else
                        {
                            await ReplyAsync("You did not reply in time. Action canceled. Try again using the `cancel` command.");
                            return;
                        }
                    }

                    await Order(toCancel.Id);
                    await ReplyAsync("Are you sure you want to cancel the order above? (Type 'yes' or 'no')");
                    var confirm = await NextMessageAsync(timeout: new TimeSpan(0, 2, 0));
                    if (confirm != null && confirm.Content.ToLower() == "yes")
                    {
                        db.DeleteRecord<Order>("Orders", toCancel.Id);
                        await ReplyAsync($"Purchase order with orderID `{toCancel.Id}` successfully canceled!");
                    }
                    else
                    {
                        if (confirm == null)
                        {
                            await ReplyAsync(
                                "You did not reply in time. Action canceled. Try again using the `cancel` command.");
                        }
                        else await ReplyAsync("Action canceled. Try again using the `cancel` command.");
                    }
                }
                catch (Exception e)
                {
                    if (e.Message == "Input string was not in a correct format.")
                    {
                        await ReplyAsync("That is not a number. Cancellation stopped. Try again.");
                    }
                    else
                    {
                        await ReplyAsync("An error occurred. Error message: " + e.Message);
                    }
                }
            }
            else
            {
                await ReplyAsync("This command is only usable in the bot's DMs.");
            }
        }
    }
}