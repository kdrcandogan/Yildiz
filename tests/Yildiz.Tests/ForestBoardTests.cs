using Yildiz.Core.Models;

namespace Yildiz.Tests;

public sealed class ForestBoardTests
{
    [Fact]
    public void Constructor_CopiesObstacleArrayDefensively_PreventingExternalMutation()
    {
        ObstacleType[,] sourceObstacles = new ObstacleType[3, 3];
        sourceObstacles[0, 1] = ObstacleType.Fence;

        var board = new ForestBoard(3, sourceObstacles);
        sourceObstacles[0, 1] = ObstacleType.Wolf;
        sourceObstacles[0, 0] = ObstacleType.Fox;

        Assert.Equal(ObstacleType.Fence, board.GetObstacleAt(new Position(0, 1)));
        Assert.Equal(ObstacleType.None, board.GetObstacleAt(new Position(0, 0)));
    }

    [Fact]
    public void Constructor_WhenStartCellHasObstacle_ThrowsArgumentException()
    {
        ObstacleType[,] obstacles = new ObstacleType[3, 3];
        obstacles[0, 0] = ObstacleType.Wolf;

        Assert.Throws<ArgumentException>(() => new ForestBoard(3, obstacles));
    }

    [Fact]
    public void Constructor_WhenHoleCellHasObstacle_ThrowsArgumentException()
    {
        ObstacleType[,] obstacles = new ObstacleType[3, 3];
        obstacles[2, 2] = ObstacleType.BarbedWire;

        Assert.Throws<ArgumentException>(() => new ForestBoard(3, obstacles));
    }

    [Fact]
    public void Constructor_WhenMatrixDimensionsMismatchSize_ThrowsArgumentException()
    {
        ObstacleType[,] mismatchedObstacles = new ObstacleType[2, 3];

        Assert.Throws<ArgumentException>(() => new ForestBoard(3, mismatchedObstacles));
    }

    [Fact]
    public void GetObstacleAt_WhenPositionOutOfBounds_ThrowsArgumentOutOfRangeException()
    {
        var board = new ForestBoard(3, new ObstacleType[3, 3]);

        Assert.Throws<ArgumentOutOfRangeException>(() => board.GetObstacleAt(new Position(-1, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => board.GetObstacleAt(new Position(0, 3)));
    }

    [Fact]
    public void DirectionExtensions_TurnRightAndTurnLeft_FollowCompassOrder()
    {
        Assert.Equal(Direction.West, Direction.South.TurnRight());
        Assert.Equal(Direction.North, Direction.West.TurnRight());
        Assert.Equal(Direction.East, Direction.North.TurnRight());
        Assert.Equal(Direction.South, Direction.East.TurnRight());

        Assert.Equal(Direction.East, Direction.South.TurnLeft());
        Assert.Equal(Direction.North, Direction.East.TurnLeft());
        Assert.Equal(Direction.West, Direction.North.TurnLeft());
        Assert.Equal(Direction.South, Direction.West.TurnLeft());
    }

    [Fact]
    public void Position_Move_AdvancesInSpecifiedDirection()
    {
        Position start = new(1, 1);

        Assert.Equal(new Position(2, 1), start.Move(Direction.South));
        Assert.Equal(new Position(0, 1), start.Move(Direction.North));
        Assert.Equal(new Position(1, 2), start.Move(Direction.East));
        Assert.Equal(new Position(1, 0), start.Move(Direction.West));
    }
}
