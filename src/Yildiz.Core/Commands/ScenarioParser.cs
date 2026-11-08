namespace Yildiz.Core.Commands;

public sealed class ScenarioParser
{
    public ScenarioParseResult Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return ScenarioParseResult.Failure("Scenario input cannot be empty.");
        }

        string[] rawTokens = input.Split(',');
        List<ScenarioCommand> commands = new(rawTokens.Length);

        for (int index = 0; index < rawTokens.Length; index++)
        {
            int commandNumber = index + 1;
            string token = rawTokens[index].Trim();

            if (string.IsNullOrEmpty(token))
            {
                return ScenarioParseResult.Failure(
                    $"Command #{commandNumber} is empty.",
                    errorCommandNumber: commandNumber,
                    invalidToken: token);
            }

            if (!TryParseToken(token, out ScenarioCommand command))
            {
                return ScenarioParseResult.Failure(
                    $"Command #{commandNumber} ('{token}') is not a valid command.",
                    errorCommandNumber: commandNumber,
                    invalidToken: token);
            }

            commands.Add(command);
        }

        if (!TryValidateSequence(commands, out int invalidCommandNumber, out string? sequenceError))
        {
            string offendingToken = rawTokens[invalidCommandNumber - 1].Trim();
            return ScenarioParseResult.Failure(
                sequenceError,
                errorCommandNumber: invalidCommandNumber,
                invalidToken: offendingToken);
        }

        return ScenarioParseResult.Success(commands);
    }

    public static bool TryValidateSequence(
        IReadOnlyList<ScenarioCommand> commands,
        out int errorCommandNumber,
        out string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(commands);

        for (int index = 0; index < commands.Count; index++)
        {
            ScenarioCommand current = commands[index];
            if (!IsPreparationCommand(current))
            {
                continue;
            }

            int currentNumber = index + 1;

            if (index == commands.Count - 1)
            {
                errorCommandNumber = currentNumber;
                errorMessage = $"Command #{currentNumber} ('{current}') cannot be the last command; it must be followed immediately by N or P.";
                return false;
            }

            ScenarioCommand next = commands[index + 1];
            if (!IsMovementCommand(next))
            {
                int nextNumber = index + 2;
                errorCommandNumber = nextNumber;
                errorMessage = $"Command #{nextNumber} ('{next}') is invalid after command #{currentNumber} ('{current}'); expected N or P.";
                return false;
            }
        }

        errorCommandNumber = 0;
        errorMessage = string.Empty;
        return true;
    }

    private static bool TryParseToken(string token, out ScenarioCommand command)
    {
        if (token.Equals("i\u0307", StringComparison.OrdinalIgnoreCase))
        {
            command = ScenarioCommand.Duck;
            return true;
        }

        if (token.Length != 1)
        {
            command = default;
            return false;
        }

        switch (token[0])
        {
            case 'N':
            case 'n':
                command = ScenarioCommand.MoveForward;
                return true;

            case 'P':
            case 'p':
                command = ScenarioCommand.MoveBackward;
                return true;

            case 'R':
            case 'r':
                command = ScenarioCommand.TurnRight;
                return true;

            case 'L':
            case 'l':
                command = ScenarioCommand.TurnLeft;
                return true;

            case 'J':
            case 'j':
                command = ScenarioCommand.Jump;
                return true;

            case 'İ':
            case 'i':
            case 'I':
            case 'ı':
                command = ScenarioCommand.Duck;
                return true;

            default:
                command = default;
                return false;
        }
    }

    private static bool IsPreparationCommand(ScenarioCommand command) =>
        command is ScenarioCommand.Jump or ScenarioCommand.Duck;

    private static bool IsMovementCommand(ScenarioCommand command) =>
        command is ScenarioCommand.MoveForward or ScenarioCommand.MoveBackward;
}
