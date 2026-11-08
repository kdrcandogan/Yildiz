using Yildiz.Core.Commands;
using Yildiz.Core.Models;

namespace Yildiz.Core.Simulation;

public sealed class SimulationEngine
{
    public SimulationResult Run(ForestBoard board, IReadOnlyList<ScenarioCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(commands);

        if (!ScenarioParser.TryValidateSequence(commands, out _, out string sequenceError))
        {
            throw new ArgumentException(sequenceError, nameof(commands));
        }

        RabbitState currentState = RabbitState.CreateInitial(board.StartPosition, board.InitialDirection);
        List<Position> visitedPositions = [currentState.Position];
        List<SimulationStep> steps = new(commands.Count);

        if (board.IsHole(currentState.Position))
        {
            return new SimulationResult(
                SimulationStatus.Success,
                currentState,
                steps,
                visitedPositions);
        }

        SimulationStatus currentStatus = SimulationStatus.Running;

        for (int index = 0; index < commands.Count; index++)
        {
            int stepNumber = index + 1;
            ScenarioCommand command = commands[index];

            (currentState, currentStatus) = ExecuteCommand(board, currentState, command, visitedPositions);

            bool isLastCommand = index == commands.Count - 1;
            if (isLastCommand && currentStatus == SimulationStatus.Running)
            {
                currentStatus = SimulationStatus.Incomplete;
            }

            steps.Add(new SimulationStep(
                stepNumber,
                command,
                currentState,
                currentStatus,
                visitedPositions));

            if (currentStatus != SimulationStatus.Running)
            {
                break;
            }
        }

        if (commands.Count == 0)
        {
            currentStatus = SimulationStatus.Incomplete;
        }

        return new SimulationResult(
            currentStatus,
            currentState,
            steps,
            visitedPositions);
    }

    private static (RabbitState NextState, SimulationStatus Status) ExecuteCommand(
        ForestBoard board,
        RabbitState currentState,
        ScenarioCommand command,
        List<Position> visitedPositions)
    {
        switch (command)
        {
            case ScenarioCommand.TurnRight:
                return (
                    currentState with { Direction = currentState.Direction.TurnRight() },
                    SimulationStatus.Running);

            case ScenarioCommand.TurnLeft:
                return (
                    currentState with { Direction = currentState.Direction.TurnLeft() },
                    SimulationStatus.Running);

            case ScenarioCommand.Jump:
                return (
                    currentState with { PendingBypass = ObstacleType.Fence },
                    SimulationStatus.Running);

            case ScenarioCommand.Duck:
                return (
                    currentState with { PendingBypass = ObstacleType.BarbedWire },
                    SimulationStatus.Running);

            case ScenarioCommand.MoveForward:
            case ScenarioCommand.MoveBackward:
                return ExecuteMovement(board, currentState, command, visitedPositions);

            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, "Unsupported scenario command.");
        }
    }

    private static (RabbitState NextState, SimulationStatus Status) ExecuteMovement(
        ForestBoard board,
        RabbitState currentState,
        ScenarioCommand movementCommand,
        List<Position> visitedPositions)
    {
        Direction movementDirection = movementCommand == ScenarioCommand.MoveForward
            ? currentState.Direction
            : currentState.Direction.GetOpposite();

        Position targetPosition = currentState.Position.Move(movementDirection);
        ObstacleType? activeBypass = currentState.PendingBypass;
        RabbitState stateWithoutBypass = currentState with { PendingBypass = null };

        if (!board.IsInside(targetPosition))
        {
            return (stateWithoutBypass, SimulationStatus.OutOfBounds);
        }

        ObstacleType obstacle = board.GetObstacleAt(targetPosition);

        switch (obstacle)
        {
            case ObstacleType.Wolf:
            case ObstacleType.Fox:
                RecordVisit(visitedPositions, targetPosition);
                RabbitState deadState = stateWithoutBypass with
                {
                    Position = targetPosition,
                    IsAlive = false
                };
                return (deadState, SimulationStatus.Dead);

            case ObstacleType.Fence:
                if (activeBypass != ObstacleType.Fence)
                {
                    return (stateWithoutBypass, SimulationStatus.Blocked);
                }
                break;

            case ObstacleType.BarbedWire:
                if (activeBypass != ObstacleType.BarbedWire)
                {
                    return (stateWithoutBypass, SimulationStatus.Blocked);
                }
                break;

            case ObstacleType.None:
                break;

            default:
                throw new InvalidOperationException($"Unsupported obstacle type: {obstacle}.");
        }

        RecordVisit(visitedPositions, targetPosition);
        RabbitState movedState = stateWithoutBypass with { Position = targetPosition };

        SimulationStatus nextStatus = board.IsHole(targetPosition)
            ? SimulationStatus.Success
            : SimulationStatus.Running;

        return (movedState, nextStatus);
    }

    private static void RecordVisit(List<Position> visitedPositions, Position position)
    {
        if (!visitedPositions.Contains(position))
        {
            visitedPositions.Add(position);
        }
    }
}
