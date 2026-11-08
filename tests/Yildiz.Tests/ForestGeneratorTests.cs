using Yildiz.Core.Commands;
using Yildiz.Core.Generation;
using Yildiz.Core.Models;
using Yildiz.Core.Pathfinding;
using Yildiz.Core.Simulation;

namespace Yildiz.Tests;

public sealed class ForestGeneratorTests
{
    private static readonly ObstacleType[] ObstacleTypes =
    [
        ObstacleType.Wolf,
        ObstacleType.Fox,
        ObstacleType.BarbedWire,
        ObstacleType.Fence
    ];

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(16)]
    [InlineData(33)]
    [InlineData(40)]
    [InlineData(50)]
    public void Generate_WithValidSizes_CreatesBoardWithExpectedDimensionsAndPositions(int size)
    {
        var generator = new ForestGenerator();

        ForestGenerationResult result = generator.Generate(size, seed: 42);

        Assert.Equal(size, result.Board.Size);
        Assert.Equal(new Position(0, 0), result.Board.StartPosition);
        Assert.Equal(Direction.South, result.Board.InitialDirection);
        Assert.Equal(new Position(size - 1, size - 1), result.Board.HolePosition);
        Assert.Equal(ObstacleType.None, result.Board.GetObstacleAt(result.Board.StartPosition));
        Assert.Equal(ObstacleType.None, result.Board.GetObstacleAt(result.Board.HolePosition));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-15)]
    public void Generate_WithNonPositiveSize_ThrowsArgumentOutOfRangeException(int invalidSize)
    {
        var generator = new ForestGenerator();

        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(invalidSize, seed: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GuaranteedPathBuilder_WithNonPositiveSize_ThrowsArgumentOutOfRangeException(int invalidSize)
    {
        var builder = new GuaranteedPathBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Build(invalidSize));
    }

    [Theory]
    [InlineData(46341)]
    [InlineData(100000)]
    [InlineData(int.MaxValue)]
    public void Generate_WithSizeExceedingMaxAllocatableSize_ThrowsArgumentOutOfRangeException(int excessiveSize)
    {
        var generator = new ForestGenerator();

        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(excessiveSize, seed: 1));
    }

    [Fact]
    public void GuaranteedPathBuilder_WithSizeCausingPathLengthOverflow_ThrowsArgumentOutOfRangeException()
    {
        var builder = new GuaranteedPathBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Build(int.MaxValue));
    }

    [Fact]
    public void Generate_SizeOne_ContainsSingleEmptyCellAndPathOfLengthOne()
    {
        var generator = new ForestGenerator();

        ForestGenerationResult result = generator.Generate(size: 1, seed: 10);

        Assert.Single(result.GuaranteedPath);
        Assert.Equal(new Position(0, 0), result.GuaranteedPath[0]);
        Assert.Equal(ObstacleType.None, result.Board.GetObstacleAt(new Position(0, 0)));

        foreach (ObstacleType obstacleType in ObstacleTypes)
        {
            Assert.Equal(0, result.Board.CountObstacles(obstacleType));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(16)]
    [InlineData(33)]
    [InlineData(40)]
    [InlineData(50)]
    public void Generate_GuaranteedPath_ContainsStartAndHoleAndUsesOnlyAdjacentObstacleFreeCells(int size)
    {
        var generator = new ForestGenerator();

        ForestGenerationResult result = generator.Generate(size, seed: 99);
        IReadOnlyList<Position> path = result.GuaranteedPath;

        Assert.Equal((2 * size) - 1, path.Count);
        Assert.Equal(result.Board.StartPosition, path[0]);
        Assert.Equal(result.Board.HolePosition, path[^1]);

        for (int i = 0; i < path.Count; i++)
        {
            Position current = path[i];
            Assert.True(result.Board.IsInside(current));
            Assert.Equal(ObstacleType.None, result.Board.GetObstacleAt(current));

            if (i > 0)
            {
                Assert.True(path[i - 1].IsAdjacentTo(current));
            }
        }
    }

    [Fact]
    public void Generate_AcrossOneHundredSeeds_PreservesAllObstacleAndPathRules()
    {
        var generator = new ForestGenerator();
        int[] sizesToTest = [1, 2, 3, 7, 16];

        foreach (int size in sizesToTest)
        {
            for (int seed = 1; seed <= 100; seed++)
            {
                ForestGenerationResult result = generator.Generate(size, seed);
                ForestBoard board = result.Board;

                Assert.Equal(new Position(0, 0), board.StartPosition);
                Assert.Equal(Direction.South, board.InitialDirection);
                Assert.Equal(new Position(size - 1, size - 1), board.HolePosition);
                Assert.Equal(ObstacleType.None, board.GetObstacleAt(board.StartPosition));
                Assert.Equal(ObstacleType.None, board.GetObstacleAt(board.HolePosition));

                int totalObstacleCountByType = 0;
                foreach (ObstacleType obstacleType in ObstacleTypes)
                {
                    int count = board.CountObstacles(obstacleType);
                    Assert.InRange(count, ForestGenerator.MinObstaclesPerType, ForestGenerator.MaxObstaclesPerType);
                    totalObstacleCountByType += count;
                }

                int occupiedCellsOnBoard = CountAllNonEmptyCells(board);
                Assert.Equal(totalObstacleCountByType, occupiedCellsOnBoard);

                Assert.Equal(board.StartPosition, result.GuaranteedPath[0]);
                Assert.Equal(board.HolePosition, result.GuaranteedPath[^1]);

                for (int i = 0; i < result.GuaranteedPath.Count; i++)
                {
                    Position pathCell = result.GuaranteedPath[i];
                    Assert.Equal(ObstacleType.None, board.GetObstacleAt(pathCell));

                    if (i > 0)
                    {
                        Assert.True(result.GuaranteedPath[i - 1].IsAdjacentTo(pathCell));
                    }
                }
            }
        }
    }

    [Fact]
    public void Generate_GuaranteedPathDerivedCommands_ReachSuccessInSimulationAcrossOneHundredSeedsAndMultipleSizes()
    {
        var pathBuilder = new GuaranteedPathBuilder();
        var generator = new ForestGenerator(pathBuilder);
        var simulationEngine = new SimulationEngine();
        int[] sizesToTest = [1, 2, 3, 7, 16, 33, 40, 50];

        foreach (int size in sizesToTest)
        {
            for (int seed = 1; seed <= 100; seed++)
            {
                ForestGenerationResult generationResult = generator.Generate(size, seed);
                IReadOnlyList<ScenarioCommand> commands = pathBuilder.BuildCommands(
                    generationResult.GuaranteedPath,
                    generationResult.Board.InitialDirection);

                SimulationResult simulationResult = simulationEngine.Run(generationResult.Board, commands);

                Assert.Equal(SimulationStatus.Success, simulationResult.Status);
                Assert.Equal(generationResult.Board.HolePosition, simulationResult.FinalPosition);
                Assert.True(simulationResult.FinalState.IsAlive);
            }
        }
    }

    [Theory]
    [InlineData(3, 12345)]
    [InlineData(7, 54321)]
    [InlineData(16, 2026)]
    public void Generate_WithSameSeedAndSize_ProducesIdenticalBoardAndGuaranteedPath(int size, int seed)
    {
        var generator = new ForestGenerator();

        ForestGenerationResult first = generator.Generate(size, seed);
        ForestGenerationResult second = generator.Generate(size, seed);

        Assert.Equal(first.GuaranteedPath, second.GuaranteedPath);

        for (int row = 0; row < size; row++)
        {
            for (int column = 0; column < size; column++)
            {
                Position position = new(row, column);
                Assert.Equal(first.Board.GetObstacleAt(position), second.Board.GetObstacleAt(position));
            }
        }
    }

    private static int CountAllNonEmptyCells(ForestBoard board)
    {
        int count = 0;

        for (int row = 0; row < board.Size; row++)
        {
            for (int column = 0; column < board.Size; column++)
            {
                if (board.GetObstacleAt(new Position(row, column)) != ObstacleType.None)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
