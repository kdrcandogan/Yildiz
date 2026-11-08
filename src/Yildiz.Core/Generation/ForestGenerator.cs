using Yildiz.Core.Models;
using Yildiz.Core.Pathfinding;

namespace Yildiz.Core.Generation;

public sealed class ForestGenerator
{
    public const int MinObstaclesPerType = 0;
    public const int MaxObstaclesPerType = 4;
    public const int MaxAllocatableSize = 46340;

    private static readonly ObstacleType[] PlaceableObstacleTypes =
    [
        ObstacleType.Wolf,
        ObstacleType.Fox,
        ObstacleType.BarbedWire,
        ObstacleType.Fence
    ];

    private readonly GuaranteedPathBuilder _pathBuilder;

    public ForestGenerator(GuaranteedPathBuilder? pathBuilder = null)
    {
        _pathBuilder = pathBuilder ?? new GuaranteedPathBuilder();
    }

    public ForestGenerationResult Generate(int size, int? seed = null)
    {
        Random random = seed.HasValue ? new Random(seed.Value) : new Random();
        return Generate(size, random);
    }

    public ForestGenerationResult Generate(int size, Random random)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(size, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, MaxAllocatableSize);
        ArgumentNullException.ThrowIfNull(random);

        IReadOnlyList<Position> guaranteedPath = _pathBuilder.Build(size, random);
        HashSet<Position> protectedPositions = new(guaranteedPath);

        List<Position> availablePositions = CollectAvailablePositions(size, protectedPositions);
        Shuffle(availablePositions, random);

        ObstacleType[] obstacleOrder = (ObstacleType[])PlaceableObstacleTypes.Clone();
        Shuffle(obstacleOrder, random);

        ObstacleType[,] obstacles = new ObstacleType[size, size];
        int availableIndex = 0;

        foreach (ObstacleType obstacleType in obstacleOrder)
        {
            int requestedCount = random.Next(MinObstaclesPerType, MaxObstaclesPerType + 1);
            int remainingCapacity = availablePositions.Count - availableIndex;
            int actualCount = Math.Min(requestedCount, remainingCapacity);

            for (int i = 0; i < actualCount; i++)
            {
                Position targetPosition = availablePositions[availableIndex++];
                obstacles[targetPosition.Row, targetPosition.Column] = obstacleType;
            }
        }

        ForestBoard board = new(size, obstacles);
        return new ForestGenerationResult(board, guaranteedPath);
    }

    private static List<Position> CollectAvailablePositions(int size, HashSet<Position> protectedPositions)
    {
        List<Position> availablePositions = [];

        for (int row = 0; row < size; row++)
        {
            for (int column = 0; column < size; column++)
            {
                Position candidate = new(row, column);
                if (!protectedPositions.Contains(candidate))
                {
                    availablePositions.Add(candidate);
                }
            }
        }

        return availablePositions;
    }

    private static void Shuffle<T>(IList<T> items, Random random)
    {
        for (int i = 0; i < items.Count - 1; i++)
        {
            int swapIndex = random.Next(i, items.Count);
            (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
        }
    }
}
