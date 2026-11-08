using Yildiz.Core.Models;

namespace Yildiz.Core.Simulation;

public sealed record SimulationResult
{
    public SimulationResult(
        SimulationStatus status,
        RabbitState finalState,
        IEnumerable<SimulationStep> steps,
        IEnumerable<Position> visitedPositions)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(visitedPositions);

        Status = status;
        FinalState = finalState;
        Steps = Array.AsReadOnly(steps.ToArray());
        VisitedPositions = Array.AsReadOnly(visitedPositions.ToArray());
    }

    public SimulationStatus Status { get; }

    public RabbitState FinalState { get; }

    public Position FinalPosition => FinalState.Position;

    public Direction FinalDirection => FinalState.Direction;

    public IReadOnlyList<SimulationStep> Steps { get; }

    public IReadOnlyList<Position> VisitedPositions { get; }
}
