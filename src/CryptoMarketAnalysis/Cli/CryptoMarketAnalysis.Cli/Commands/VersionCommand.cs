using Spectre.Console;
using Spectre.Console.Cli;

namespace CryptoMarketAnalysis.Cli.Commands;

public sealed class VersionCommand : Command
{
    protected override int Execute(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        AnsiConsole.MarkupLine("[green]CryptoMarketAnalysis CLI[/]");
        AnsiConsole.MarkupLine("Version: 1.0.0");

        return 0;
    }
}