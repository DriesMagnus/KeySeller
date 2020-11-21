using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using KeySeller.StaticVars;
using Microsoft.Extensions.DependencyInjection;

namespace KeySeller
{
    public class CommandHandler
    {
        private DiscordSocketClient _client;

        private CommandService _service;

        private IServiceProvider _services;

        private readonly DiscordColor _discordColor = new DiscordColor();

        public CommandHandler(DiscordSocketClient client)
        {
            _client = client;

            _service = new CommandService();

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<InteractiveService>()
                .BuildServiceProvider();

            _service.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;
            if (msg == null) return;

            var context = new SocketCommandContext(_client, msg);

            var argPos = 0;
            var prefix = ';';

            #region Help Command

            if (msg.Content.ToLower().Contains(prefix + "help"))
            {
                if (_service.Commands.Count(x => msg.Content.ToLower().Insert(msg.Content.Length - 1, "  ").Substring(6).Equals(x.Name.ToLower())) > 0)
                {
                    var cmd = _service.Commands.First(x => msg.Content.ToLower().Substring(6).Equals(x.Name.ToLower()));
                    var embedBuilder = new EmbedBuilder
                    {
                        Color = _discordColor.LightBlue,
                        Author = new EmbedAuthorBuilder
                        {
                            Name = cmd.Name.First().ToString().ToUpper() + cmd.Name.Substring(1),
                            IconUrl =
                                "https://cdn.discordapp.com/avatars/736541614440448020/dd7b0581dcff206877ae4632c911ab05.png?size=1024"
                        }
                    };

                    var description = "";

                    if (cmd.Summary != null)
                    {
                        description += $"`{cmd.Name}`: {cmd.Summary}\n\n";
                    }
                    else
                    {
                        description += $"`{cmd.Name}`: No description provided.\n\n";
                    }

                    var syntax = cmd.Parameters.Aggregate($"`{cmd.Name}", (current, p) => current + $" [{p.Name}]") + "`\n";
                    description += "**Arguments:**\n" + syntax;
                    foreach (var parameter in cmd.Parameters)
                    {
                        if (parameter.Summary != null)
                        {
                            description += $"`{parameter.Name} ({parameter.Type.Name})`: {parameter.Summary}\n";
                        }
                        else
                        {
                            description += $"`{parameter.Name} ({parameter.Type.Name})`: No description provided.\n";
                        }
                    }

                    embedBuilder.WithDescription(description);
                    await context.Channel.SendMessageAsync(embed: embedBuilder.Build());
                }
                else
                {
                    var embedBuilder = new EmbedBuilder
                    {
                        Color = _discordColor.LightBlue,
                        Author = new EmbedAuthorBuilder
                        {
                            Name = "Commands",
                            IconUrl =
                                "https://cdn.discordapp.com/avatars/736541614440448020/dd7b0581dcff206877ae4632c911ab05.png?size=1024"
                        }
                    };

                    var description =
                        $"**Use the `{prefix}help [commandname]` command to see more details about that command.**\n\n";
                    foreach (var command in _service.Commands)
                    {
                        var isExecutable = true;
                        foreach (var precon in command.Preconditions)
                        {
                            var result = await precon
                                .CheckPermissionsAsync(context, command, _services);
                            if (result.Error.HasValue) isExecutable = false;
                        }

                        if (isExecutable)
                        {
                            description += $"`{command.Name}`, ";
                        }
                    }

                    if (description.Length > 0) description = description[..^2];
                    embedBuilder.WithDescription(description);
                    await context.Channel.SendMessageAsync(embed: embedBuilder.Build());
                }
            }

            #endregion

            if (msg.HasCharPrefix(prefix, ref argPos))
            {
                var result = await _service.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    Console.WriteLine(result.ErrorReason);
                    await context.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }
    }
}