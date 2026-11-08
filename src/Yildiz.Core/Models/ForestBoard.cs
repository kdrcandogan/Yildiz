namespace Yildiz.Core.Models;

public sealed class ForestBoard
{
    private readonly ObstacleType[,] _obstacles;

    public ForestBoard(int size, ObstacleType[,] obstacles)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(size, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, Generation.ForestGenerator.MaxAllocatableSize);
        ArgumentNullException.ThrowIfNull(obstacles);

        if (obstacles.GetLength(0) != size || obstacles.GetLength(1) != size)
        {
            throw new ArgumentException("Obstacle matrix dimensions must match the forest size.", nameof(obstacles));
        }

        Size = size;
        StartPosition = new Position(0, 0);
        HolePosition = new Position(size - 1, size - 1);

        if (obstacles[StartPosition.Row, StartPosition.Column] != ObstacleType.None)
        {
            throw new ArgumentException("Start position cannot contain an obstacle.", nameof(obstacles));
        }

        if (obstacles[HolePosition.Row, HolePosition.Column] != ObstacleType.None)
        {
            throw new ArgumentException("Hole position cannot contain an obstacle.", nameof(obstacles));
        }

        _obstacles = (ObstacleType[,])obstacles.Clone();
    }

    public int Size { get; }

    public Position StartPosition { get; }

    public Direction InitialDirection => Direction.South;

    public Position HolePosition { get; }

    public bool IsInside(Position position) =>
        position.Row >= 0 &&
        position.Row < Size &&
        position.Column >= 0 &&
        position.Column < Size;

    public bool IsStart(Position position) => position == StartPosition;

    public bool IsHole(Position position) => position == HolePosition;

    public ObstacleType GetObstacleAt(Position position)
    {
        if (!IsInside(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Position is outside the forest boundaries.");
        }

        return _obstacles[position.Row, position.Column];
    }

    public int CountObstacles(ObstacleType obstacleType)
    {
        int count = 0;

        for (int row = 0; row < Size; row++)
        {
            for (int column = 0; column < Size; column++)
            {
                if (_obstacles[row, column] == obstacleType)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
