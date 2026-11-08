namespace Yildiz.Core.Models;

public readonly record struct RabbitState(
    Position Position,
    Direction Direction,
    bool IsAlive,
    ObstacleType? PendingBypass = null)
{
    public bool HasPendingBypass => PendingBypass.HasValue;

    public static RabbitState CreateInitial(
        Position startPosition,
        Direction initialDirection = Direction.South) =>
        new(
            Position: startPosition,
            Direction: initialDirection,
            IsAlive: true,
            PendingBypass: null);
}
