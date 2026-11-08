namespace Yildiz.Core.Commands;

public sealed record ScenarioParseResult
{
    private ScenarioParseResult(
        bool isSuccess,
        IReadOnlyList<ScenarioCommand> commands,
        string? errorMessage,
        int? errorCommandNumber,
        string? invalidToken)
    {
        IsSuccess = isSuccess;
        Commands = commands;
        ErrorMessage = errorMessage;
        ErrorCommandNumber = errorCommandNumber;
        InvalidToken = invalidToken;
    }

    public bool IsSuccess { get; }

    public IReadOnlyList<ScenarioCommand> Commands { get; }

    public string? ErrorMessage { get; }

    public int? ErrorCommandNumber { get; }

    public string? InvalidToken { get; }

    public static ScenarioParseResult Success(IReadOnlyList<ScenarioCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        return new ScenarioParseResult(
            isSuccess: true,
            commands: Array.AsReadOnly(commands.ToArray()),
            errorMessage: null,
            errorCommandNumber: null,
            invalidToken: null);
    }

    public static ScenarioParseResult Failure(
        string errorMessage,
        int? errorCommandNumber = null,
        string? invalidToken = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new ScenarioParseResult(
            isSuccess: false,
            commands: Array.AsReadOnly(Array.Empty<ScenarioCommand>()),
            errorMessage: errorMessage,
            errorCommandNumber: errorCommandNumber,
            invalidToken: invalidToken);
    }
}
