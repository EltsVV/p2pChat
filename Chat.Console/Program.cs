using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Chat.Console.Commands;
using Chat.Security.Encryption;
using Chat.Core.Commands;
using Chat.Services;
using Chat.Network.Services;
using Chat.Core.Interfaces;
using Chat.Console.Services;
using Microsoft.Extensions.Logging;

namespace Chat.Console;

class Program
{
    private static IServiceProvider _serviceProvider = null!;
    private static IMediator _mediator = null!;
    private static IUIService _uiService = null!;
    private static List<IChatCommand> _commands = null!;
    private static ILogger<Program> _logger = null!;
    private static bool _isRunning = true;
    private static int _udpPort = 12345;
    private static int _tcpPort = 12346;

    static async Task Main(string[] args)
    {
        try
        {
            if (args.Length >= 2)
            {
                if (int.TryParse(args[0], out int udpPort) && int.TryParse(args[1], out int tcpPort))
                {
                    _udpPort = udpPort;
                    _tcpPort = tcpPort;
                }
            }

            System.Console.Write("Enter your username: ");
            var username = System.Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(username))
            {
                System.Console.WriteLine("Username cannot be empty");
                return;
            }

            ConfigureServices(username);

            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
            _logger.LogInformation("Application starting for user {Username}", username);

            await InitializeServices(username);
            RegisterCommands();

            _uiService.DisplaySystemMessage($"Welcome to Distributed Chat, {username}!");
            _uiService.DisplaySystemMessage("Type /help for available commands");

            while (_isRunning)
            {
                var input = System.Console.ReadLine();
                if (!string.IsNullOrEmpty(input))
                {
                    await ProcessInput(input);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Application terminated unexpectedly");
            System.Console.WriteLine($"Fatal error: {ex.Message}");
        }
    }

    private static void ConfigureServices(string username)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(Chat.Services.Handlers.SendBroadcastMessageHandler).Assembly,
                typeof(Program).Assembly
            );
        });

        services.AddSingleton<IUIService, ConsoleUIService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        services.AddSingleton<INetworkService>(provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            var userService = provider.GetRequiredService<IUserService>();
            var encryptionService = provider.GetRequiredService<IEncryptionService>();
            var logger = provider.GetRequiredService<ILogger<NetworkService>>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

            userService.SetCurrentUser(username, Core.Enums.UserRole.Admin);

            return new NetworkService(username, _udpPort, _tcpPort, mediator, userService, encryptionService, logger, loggerFactory);
        });

        services.AddSingleton<NetworkService>(provider =>
            (NetworkService)provider.GetRequiredService<INetworkService>());

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        _uiService = _serviceProvider.GetRequiredService<IUIService>();
    }

    private static async Task InitializeServices(string username)
    {
        var networkService = _serviceProvider.GetRequiredService<INetworkService>();
        networkService.Start();

        await Task.Delay(1000);
    }

    private static void RegisterCommands()
    {
        var networkService = _serviceProvider.GetRequiredService<INetworkService>();

        _commands = new List<IChatCommand>
        {
            new P2PMessageCommand(_mediator, _uiService),
            new BanUserConsoleCommand(_mediator, _uiService),
            new UnbanUserConsoleCommand(_mediator, _uiService),
            new UsersCommand(_mediator, _uiService, networkService),
        };

        var helpCommand = new HelpCommand(new List<IChatCommand>(_commands), _uiService);
        _commands.Add(helpCommand);
    }

    private static async Task ProcessInput(string input)
    {
        try
        {
            if (input.StartsWith('/')) await ExecuteCommand(input);
            else await _mediator.Send(new SendBroadcastMessageCommand(input));
        }
        catch (Exception ex)
        {
            _uiService.DisplayErrorMessage($"Error: {ex.Message}");
        }
    }

    private static async Task ExecuteCommand(string input)
    {
        try
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var commandName = parts[0].ToLower();
            var args = parts.Skip(1).ToArray();

            var command = _commands.FirstOrDefault(c => c.Command.ToLower() == commandName);
            if (command != null) await command.ExecuteAsync(args);
            else _uiService.DisplayErrorMessage($"Unknown command: {commandName}. Type /help for available commands.");
        }
        catch (Exception ex)
        {
            _uiService.DisplayErrorMessage($"Command error: {ex.Message}");
        }
    }
}