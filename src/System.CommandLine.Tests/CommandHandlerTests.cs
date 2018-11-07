﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.CommandLine.Tests
{
    public class CommandHandlerTests
    {
        private readonly TestConsole _console = new TestConsole();

        [Fact]
        public async Task Specific_invocation_behavior_can_be_specified_in_the_command()
        {
            var wasCalled = false;

            var command = new Command("command");
            command.Handler = CommandHandler.Create(() => wasCalled = true);

            var parser = new Parser(command);

            await parser.InvokeAsync("command", _console);

            wasCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Method_parameters_on_the_invoked_method_are_bound_to_matching_option_names()
        {
            var wasCalled = false;

            void Execute(string name, int age)
            {
                wasCalled = true;
                name.Should().Be("Gandalf");
                age.Should().Be(425);
            }

            var command = new Command("command");
            command.AddOption(
                new Option("--name",
                           argument: new Argument { Arity = ArgumentArity.ExactlyOne }));
            command.AddOption(
                new Option("--age",
                           argument: new Argument<int>()));
            command.Handler = CommandHandler.Create<string, int>(Execute);

            await command.InvokeAsync("command --age 425 --name Gandalf", _console);

            wasCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Method_parameters_on_the_invoked_method_can_be_bound_to_hyphenated_option_names()
        {
            var wasCalled = false;

            void Execute(string firstName)
            {
                wasCalled = true;
                firstName.Should().Be("Gandalf");
            }

            var command = new Command("command");
            command.AddOption(new Option("--first-name",
                                         argument: new Argument { Arity = ArgumentArity.ExactlyOne }));
            command.Handler = CommandHandler.Create<string>(Execute);

            await command.InvokeAsync("command --first-name Gandalf", _console);

            wasCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Method_parameters_on_the_invoked_method_can_be_bound_to_option_names_case_insensitively()
        {
            var wasCalled = false;

            void Execute(string name, int Age)
            {
                wasCalled = true;
                name.Should().Be("Gandalf");
                Age.Should().Be(425);
            }

            var command = new Command("command");
            command.AddOption(new Option("--NAME", argument: new Argument { Arity = ArgumentArity.ExactlyOne }));
            command.AddOption(new Option("--age", argument: new Argument<int>()));
            command.Handler = CommandHandler.Create<string, int>(Execute);

            await command.InvokeAsync("command --age 425 --NAME Gandalf", _console);

            wasCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Method_parameters_on_the_invoked_method_do_not_need_to_be_matched()
        {
            var wasCalled = false;

            void Execute(string name, int age)
            {
                wasCalled = true;
                name.Should().Be(null);
                age.Should().Be(0);
            }

            var command = new Command("command");
            command.AddOption(new Option("--name", argument: new Argument { Arity = ArgumentArity.ExactlyOne }));
            command.AddOption(new Option("--age", argument: new Argument<int>()));
            command.Handler = CommandHandler.Create<string, int>(Execute);

            await command.InvokeAsync("command", _console);

            wasCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Method_parameters_on_the_invoked_method_can_be_bound_to_option_names_by_alias()
        {
            var wasCalled = false;

            void Execute(string name, int Age)
            {
                wasCalled = true;
                name.Should().Be("Gandalf");
                Age.Should().Be(425);
            }

            var command = new Command("command");
            command.AddOption(new Option(new[] { "-n", "--NAME" }, argument: new Argument { Arity = ArgumentArity.ExactlyOne }));
            command.AddOption(new Option(new[] { "-a", "--age" }, argument: new Argument<int>()));
            command.Handler = CommandHandler.Create<string, int>(Execute);

            await command.InvokeAsync("command -a 425 -n Gandalf", _console);

            wasCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Method_parameters_on_the_invoked_lambda_are_bound_to_matching_option_names()
        {
            var wasCalled = false;

            var parser =
                new CommandLineBuilder()
                    .AddCommand(
                        "command", "",
                        cmd =>
                        {
                            cmd
                                .AddOption("--name", "", a => a.ExactlyOne())
                                .OnExecute<string, int>((name, age) =>
                                {
                                    wasCalled = true;
                                    name.Should().Be("Gandalf");
                                    age.Should().Be(425);
                                })
                                .AddOption("--age", "", a => a.ParseArgumentsAs<int>());
                        })
                    .Build();

            await parser.InvokeAsync("command --age 425 --name Gandalf", _console);

            wasCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Method_parameters_of_type_ParseResult_receive_the_current_ParseResult_instance()
        {
            var wasCalled = false;

            var parser =
                new CommandLineBuilder()
                    .AddCommand(
                        "command", "",
                        cmd =>
                        {
                            cmd.AddOption("-x", "", args => args.ParseArgumentsAs<int>())
                               .OnExecute<ParseResult>(result =>
                               {
                                   wasCalled = true;
                                   result.ValueForOption("-x").Should().Be(123);
                               });
                        })
                    .Build();

            await parser.InvokeAsync("command -x 123", _console);

            wasCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Method_parameters_of_type_IConsole_receive_the_current_console_instance()
        {
            var wasCalled = false;

            var parser =
                new CommandLineBuilder()
                    .AddCommand(
                        "command", "",
                        cmd =>
                        {
                            cmd.AddOption("-x", "", args => args.ParseArgumentsAs<int>())
                               .OnExecute<IConsole>(console =>
                               {
                                   wasCalled = true;
                                   console.Out.Write("Hello!");
                               });
                        })
                    .Build();

            await parser.InvokeAsync("command", _console);

            wasCalled.Should().BeTrue();
            _console.Out.ToString().Should().Be("Hello!");
        }

        [Fact]
        public async Task Method_parameters_of_type_InvocationContext_receive_the_current_InvocationContext_instance()
        {
            var wasCalled = false;

            var parser =
                new CommandLineBuilder()
                    .AddCommand(
                        "command", "",
                        cmd =>
                        {
                            cmd.AddOption("-x", "", args => args.ParseArgumentsAs<int>())
                               .OnExecute<InvocationContext>(context =>
                               {
                                   wasCalled = true;
                                   context.ParseResult.ValueForOption("-x").Should().Be(123);
                               });
                        })
                    .Build();

            await parser.InvokeAsync("command -x 123", _console);

            wasCalled.Should().BeTrue();
        }
    }
}