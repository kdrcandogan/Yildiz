using Yildiz.Core.Commands;
using Yildiz.Core.Models;

namespace Yildiz.Core.Simulation;

public sealed record SimulationStep
{
    public SimulationStep(
        int stepNumber,
        ScenarioCommand command,
        RabbitState rabbitState,
        SimulationStatus status,
        IEnumerable<Position> visitedPositions)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(stepNumber, 0);
        ArgumentNullException.ThrowIfNull(visitedPositions);

        StepNumber = stepNumber;
        Command = command;
        RabbitState = rabbitState;
        Status = status;
        VisitedPositions = Array.AsReadOnly(visitedPositions.ToArray());
    }

    public int StepNumber { get; }

    public ScenarioCommand Command { get; }

    public RabbitState RabbitState { get; }

    public Position Position => RabbitState.Position;

    public Direction Direction => RabbitState.Direction;

    public bool IsAlive => RabbitState.IsAlive;

    public ObstacleType? PendingBypass => RabbitState.PendingBypass;

    public SimulationStatus Status { get; }

    public IReadOnlyList<Position> VisitedPositions { get; }
}
