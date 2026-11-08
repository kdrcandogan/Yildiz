using Yildiz.Core.Commands;
using Yildiz.Core.Models;

namespace Yildiz.Core.Pathfinding;

public sealed class GuaranteedPathBuilder
{
    public IReadOnlyList<Position> Build(int size, Random? random = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(size, 0);

        long totalCellsInPath = (2L * size) - 1;
        if (totalCellsInPath > Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                size,
                "Forest size is too large to construct a guaranteed path.");
        }

        Random rng = random ?? Random.Shared;
        List<Position> path = new((int)totalCellsInPath);

        Position current = new(0, 0);
        Position target = new(size - 1, size - 1);
        path.Add(current);

        while (current != target)
        {
            bool canMoveSouth = current.Row < target.Row;
            bool canMoveEast = current.Column < target.Column;

            Direction nextDirection = (canMoveSouth, canMoveEast) switch
            {
                (true, true) => rng.Next(2) == 0 ? Direction.South : Direction.East,
                (true, false) => Direction.South,
                (false, true) => Direction.East,
                _ => throw new InvalidOperationException("Cannot advance further along the guaranteed path.")
            };

            current = current.Move(nextDirection);
            path.Add(current);
        }

        return path.AsReadOnly();
    }

    public IReadOnlyList<ScenarioCommand> BuildCommands(
        IReadOnlyList<Position> path,
        Direction initialDirection = Direction.South)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (path.Count == 0)
        {
            throw new ArgumentException("Path cannot be empty.", nameof(path));
        }

        List<ScenarioCommand> commands = [];
        Direction currentDirection = initialDirection;

        for (int index = 1; index < path.Count; index++)
        {
            Position previous = path[index - 1];
            Position target = path[index];

            Direction requiredDirection = GetRequiredDirection(previous, target);
            AppendTurnCommands(commands, currentDirection, requiredDirection);
            commands.Add(ScenarioCommand.MoveForward);
            currentDirection = requiredDirection;
        }

        return commands.AsReadOnly();
    }

    private static Direction GetRequiredDirection(Position from, Position to)
    {
        int rowDelta = to.Row - from.Row;
        int columnDelta = to.Column - from.Column;

        return (rowDelta, columnDelta) switch
        {
            (-1, 0) => Direction.North,
            (0, 1) => Direction.East,
            (1, 0) => Direction.South,
            (0, -1) => Direction.West,
            _ => throw new ArgumentException($"Positions {from} and {to} are not orthogonally adjacent.")
        };
    }

    private static void AppendTurnCommands(
        List<ScenarioCommand> commands,
        Direction currentDirection,
        Direction requiredDirection)
    {
        if (currentDirection == requiredDirection)
        {
            return;
        }

        if (currentDirection.TurnLeft() == requiredDirection)
        {
            commands.Add(ScenarioCommand.TurnLeft);
            return;
        }

        if (currentDirection.TurnRight() == requiredDirection)
        {
            commands.Add(ScenarioCommand.TurnRight);
            return;
        }

        commands.Add(ScenarioCommand.TurnRight);
        commands.Add(ScenarioCommand.TurnRight);
    }
}
