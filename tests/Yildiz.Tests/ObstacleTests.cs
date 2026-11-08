using Yildiz.Core.Commands;
using Yildiz.Core.Models;
using Yildiz.Core.Simulation;

namespace Yildiz.Tests;

public sealed class ObstacleTests
{
    private readonly SimulationEngine _engine = new();

    [Fact]
    public void Fence_WithMoveForwardAlone_EndsWithBlockedAndPreservesPreviousPosition()
    {
        ObstacleType[,] obstacles = new ObstacleType[3, 3];
        obstacles[1, 0] = ObstacleType.Fence;
        var board = new ForestBoard(3, obstacles);

        SimulationResult result = _engine.Run(
            board,
            [
                ScenarioCommand.MoveForward,
                ScenarioCommand.MoveForward,
                ScenarioCommand.TurnLeft
            ]);

        Assert.Equal(SimulationStatus.Blocked, result.Status);
        Assert.Single(result.Steps);
        Assert.Equal(new Position(0, 0), result.FinalPosition);
        Assert.True(result.FinalState.IsAlive);
    }

    [Fact]
    public void Fence_WithJumpThenMoveForward_SuccessfullyBypassesFence()
    {
        ObstacleType[,] obstacles = new ObstacleType[3, 3];
        obstacles[1, 0] = ObstacleType.Fence;
        var board = new ForestBoard(3, obstacles);

        SimulationResult result = _engine.Run(
            board,
            [
                ScenarioCommand.Jump,
                ScenarioCommand.MoveForward
            ]);

        Assert.Equal(2, result.Steps.Count);
        Assert.Equal(ObstacleType.Fence, result.Steps[0].PendingBypass);
        Assert.Equal(new Position(0, 0), result.Steps[0].Position);

        Assert.Null(result.Steps[1].PendingBypass);
        Assert.Equal(new Position(1, 0), result.FinalPosition);
        Assert.Equal(ObstacleType.Fence, board.GetObstacleAt(new Position(1, 0)));
    }

    [Fact]
    public void BarbedWire_WithMoveForwardAlone_EndsWithBlockedAndPreservesPreviousPosition()
    {
        ObstacleType[,] obstacles = new ObstacleType[3, 3];
        obstacles[1, 0] = ObstacleType.BarbedWire;
        var board = new ForestBoard(3, obstacles);

        SimulationResult result = _engine.Run(
            board,
            [
                ScenarioCommand.MoveForward,
                ScenarioCommand.TurnLeft
            ]);

        Assert.Equal(SimulationStatus.Blocked, result.Status);
        Assert.Single(result.Steps);
        Assert.Equal(new Position(0, 0), result.FinalPosition);
    }

    [Fact]
    public void BarbedWire_WithDuckThenMoveForward_SuccessfullyBypassesBarbedWire()
    {
        ObstacleType[,] obstacles = new ObstacleType[3, 3];
        obstacles[1, 0] = ObstacleType.BarbedWire;
        var board = new ForestBoard(3, obstacles);

        SimulationResult result = _engine.Run(
            board,
            [
                ScenarioCommand.Duck,
                ScenarioCommand.MoveForward
            ]);

        Assert.Equal(2, result.Steps.Count);
        Assert.Equal(ObstacleType.BarbedWire, result.Steps[0].PendingBypass);
        Assert.Null(result.Steps[1].PendingBypass);
        Assert.Equal(new Position(1, 0), result.FinalPosition);
        Assert.Equal(ObstacleType.BarbedWire, board.GetObstacleAt(new Position(1, 0)));
    }

    [Theory]
    [InlineData(ObstacleType.BarbedWire, ScenarioCommand.Jump)]
    [InlineData(ObstacleType.Fence, ScenarioCommand.Duck)]
    public void WrongBypassPreparation_EndsWithBlocked(
        ObstacleType obstacleAtTarget,
        ScenarioCommand wrongPreparation)
    {
        ObstacleType[,] obstacles = new ObstacleType[3, 3];
        obstacles[1, 0] = obstacleAtTarget;
        var board = new ForestBoard(3, obstacles);

        SimulationResult result = _engine.Run(
            board,
            [
                wrongPreparation,
                ScenarioCommand.MoveForward,
                ScenarioCommand.MoveForward
            ]);

        Assert.Equal(SimulationStatus.Blocked, result.Status);
        Assert.Equal(2, result.Steps.Count);
        Assert.Equal(new Position(0, 0), result.FinalPosition);
        Assert.Null(result.FinalState.PendingBypass);
    }

    [Fact]
    public void BypassPermission_IsConsumedAfterSingleMove_EvenOnEmptyOrObstacleCell()
    {
        ObstacleType[,] consecutiveFences = new ObstacleType[3, 3];
        consecutiveFences[1, 0] = ObstacleType.Fence;
        consecutiveFences[2, 0] = ObstacleType.Fence;
        var twoFenceBoard = new ForestBoard(3, consecutiveFences);

        SimulationResult twoFenceResult = _engine.Run(
            twoFenceBoard,
            [
                ScenarioCommand.Jump,
                ScenarioCommand.MoveForward,
                ScenarioCommand.MoveForward
            ]);

        Assert.Equal(SimulationStatus.Blocked, twoFenceResult.Status);
        Assert.Equal(3, twoFenceResult.Steps.Count);
        Assert.Equal(new Position(1, 0), twoFenceResult.FinalPosition);

        ObstacleType[,] emptyThenFence = new ObstacleType[3, 3];
        emptyThenFence[2, 0] = ObstacleType.Fence;
        var emptyThenFenceBoard = new ForestBoard(3, emptyThenFence);

        SimulationResult emptyThenFenceResult = _engine.Run(
            emptyThenFenceBoard,
            [
                ScenarioCommand.Jump,
                ScenarioCommand.MoveForward,
                ScenarioCommand.MoveForward
            ]);

        Assert.Equal(SimulationStatus.Blocked, emptyThenFenceResult.Status);
        Assert.Equal(3, emptyThenFenceResult.Steps.Count);
        Assert.Null(emptyThenFenceResult.Steps[1].PendingBypass);
        Assert.Equal(new Position(1, 0), emptyThenFenceResult.FinalPosition);
    }

    [Theory]
    [InlineData(ObstacleType.Wolf)]
    [InlineData(ObstacleType.Fox)]
    public void PredatorCell_WithNormalMoveOrJumpOrDuck_KillsRabbitStopsSimulationAndPreservesObstacle(
        ObstacleType predator)
    {
        ObstacleType[,] obstacles = new ObstacleType[3, 3];
        obstacles[1, 0] = predator;
        var board = new ForestBoard(3, obstacles);

        SimulationResult directMoveResult = _engine.Run(
            board,
            [
                ScenarioCommand.MoveForward,
                ScenarioCommand.MoveForward
            ]);

        Assert.Equal(SimulationStatus.Dead, directMoveResult.Status);
        Assert.Single(directMoveResult.Steps);
        Assert.False(directMoveResult.FinalState.IsAlive);
        Assert.Equal(new Position(1, 0), directMoveResult.FinalPosition);
        Assert.Equal(predator, board.GetObstacleAt(new Position(1, 0)));

        SimulationResult jumpMoveResult = _engine.Run(
            board,
            [
                ScenarioCommand.Jump,
                ScenarioCommand.MoveForward,
                ScenarioCommand.MoveForward
            ]);

        Assert.Equal(SimulationStatus.Dead, jumpMoveResult.Status);
        Assert.Equal(2, jumpMoveResult.Steps.Count);
        Assert.False(jumpMoveResult.FinalState.IsAlive);
        Assert.Equal(new Position(1, 0), jumpMoveResult.FinalPosition);
        Assert.Equal(predator, board.GetObstacleAt(new Position(1, 0)));
    }

    [Fact]
    public void TurnLeft_Jump_MoveForward_TurnsEastAndBypassesFenceInFront()
    {
        ObstacleType[,] obstacles = new ObstacleType[3, 3];
        obstacles[0, 1] = ObstacleType.Fence;
        var board = new ForestBoard(3, obstacles);
        var parser = new ScenarioParser();

        ScenarioParseResult parseResult = parser.Parse("L,J,N");
        Assert.True(parseResult.IsSuccess);

        SimulationResult result = _engine.Run(board, parseResult.Commands);

        Assert.Equal(3, result.Steps.Count);
        Assert.Equal(Direction.East, result.Steps[0].Direction);
        Assert.Equal(new Position(0, 0), result.Steps[0].Position);
        Assert.Equal(ObstacleType.Fence, result.Steps[1].PendingBypass);
        Assert.Equal(new Position(0, 1), result.FinalPosition);
        Assert.Equal(Direction.East, result.FinalDirection);
        Assert.Null(result.FinalState.PendingBypass);
        Assert.True(result.FinalState.IsAlive);
    }

    [Fact]
    public void TurnLeft_Duck_MoveForward_TurnsEastAndBypassesBarbedWireInFront()
    {
        ObstacleType[,] obstacles = new ObstacleType[3, 3];
        obstacles[0, 1] = ObstacleType.BarbedWire;
        var board = new ForestBoard(3, obstacles);
        var parser = new ScenarioParser();

        ScenarioParseResult parseResult = parser.Parse("L,İ,N");
        Assert.True(parseResult.IsSuccess);

        SimulationResult result = _engine.Run(board, parseResult.Commands);

        Assert.Equal(3, result.Steps.Count);
        Assert.Equal(Direction.East, result.Steps[0].Direction);
        Assert.Equal(ObstacleType.BarbedWire, result.Steps[1].PendingBypass);
        Assert.Equal(new Position(0, 1), result.FinalPosition);
        Assert.Equal(Direction.East, result.FinalDirection);
        Assert.Null(result.FinalState.PendingBypass);
        Assert.True(result.FinalState.IsAlive);
    }

    [Fact]
    public void TurnRight_Duck_MoveForward_FromAppropriatePosition_TurnsAndBypassesBarbedWire()
    {
        ObstacleType[,] obstacles = new ObstacleType[3, 3];
        obstacles[1, 1] = ObstacleType.BarbedWire;
        var board = new ForestBoard(3, obstacles);
        var parser = new ScenarioParser();

        ScenarioParseResult parseResult = parser.Parse("L,N,R,İ,N");
        Assert.True(parseResult.IsSuccess);

        SimulationResult result = _engine.Run(board, parseResult.Commands);

        Assert.Equal(5, result.Steps.Count);
        Assert.Equal(new Position(0, 1), result.Steps[1].Position);
        Assert.Equal(Direction.East, result.Steps[1].Direction);
        Assert.Equal(Direction.South, result.Steps[2].Direction);
        Assert.Equal(ObstacleType.BarbedWire, result.Steps[3].PendingBypass);
        Assert.Equal(new Position(1, 1), result.FinalPosition);
        Assert.Equal(Direction.South, result.FinalDirection);
        Assert.Null(result.FinalState.PendingBypass);
        Assert.True(result.FinalState.IsAlive);
    }

    [Theory]
    [InlineData(ScenarioCommand.Jump, ScenarioCommand.TurnLeft)]
    [InlineData(ScenarioCommand.Duck, ScenarioCommand.TurnRight)]
    public void Run_WhenPreparationFollowedByTurnInsteadOfMove_ThrowsArgumentException(
        ScenarioCommand preparation,
        ScenarioCommand turnCommand)
    {
        var board = new ForestBoard(3, new ObstacleType[3, 3]);

        Assert.Throws<ArgumentException>(() =>
            _engine.Run(board, [preparation, turnCommand, ScenarioCommand.MoveForward]));
    }
}
