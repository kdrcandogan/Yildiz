using Yildiz.Core.Commands;
using Yildiz.Core.Models;
using Yildiz.Core.Simulation;

namespace Yildiz.Tests;

public sealed class MovementTests
{
    private readonly SimulationEngine _engine = new();

    [Fact]
    public void InitialState_StartsAtTopLeftFacingSouthAliveWithNoPendingBypass()
    {
        ForestBoard board = CreateEmptyBoard(3);

        RabbitState initial = RabbitState.CreateInitial(board.StartPosition, board.InitialDirection);

        Assert.Equal(new Position(0, 0), initial.Position);
        Assert.Equal(Direction.South, initial.Direction);
        Assert.True(initial.IsAlive);
        Assert.Null(initial.PendingBypass);
    }

    [Fact]
    public void TurnLeft_WhenFacingSouth_ChangesDirectionToEastWithoutMoving()
    {
        ForestBoard board = CreateEmptyBoard(3);

        SimulationResult result = _engine.Run(board, [ScenarioCommand.TurnLeft]);

        Assert.Equal(new Position(0, 0), result.FinalPosition);
        Assert.Equal(Direction.East, result.FinalDirection);
        Assert.Equal(SimulationStatus.Incomplete, result.Status);
    }

    [Fact]
    public void TurnRight_WhenFacingSouth_ChangesDirectionToWestWithoutMoving()
    {
        ForestBoard board = CreateEmptyBoard(3);

        SimulationResult result = _engine.Run(board, [ScenarioCommand.TurnRight]);

        Assert.Equal(new Position(0, 0), result.FinalPosition);
        Assert.Equal(Direction.West, result.FinalDirection);
        Assert.Equal(SimulationStatus.Incomplete, result.Status);
    }

    [Fact]
    public void FourRightTurns_OrFourLeftTurns_ReturnToInitialSouthDirection()
    {
        ForestBoard board = CreateEmptyBoard(3);

        SimulationResult rightTurns = _engine.Run(
            board,
            [
                ScenarioCommand.TurnRight,
                ScenarioCommand.TurnRight,
                ScenarioCommand.TurnRight,
                ScenarioCommand.TurnRight
            ]);

        SimulationResult leftTurns = _engine.Run(
            board,
            [
                ScenarioCommand.TurnLeft,
                ScenarioCommand.TurnLeft,
                ScenarioCommand.TurnLeft,
                ScenarioCommand.TurnLeft
            ]);

        Assert.Equal(Direction.South, rightTurns.FinalDirection);
        Assert.Equal(new Position(0, 0), rightTurns.FinalPosition);

        Assert.Equal(Direction.South, leftTurns.FinalDirection);
        Assert.Equal(new Position(0, 0), leftTurns.FinalPosition);
    }

    [Fact]
    public void MoveForwardAndMoveBackward_MoveInCorrectDirectionsWithoutChangingFacingDirection()
    {
        ForestBoard board = CreateEmptyBoard(3);

        SimulationResult result = _engine.Run(
            board,
            [
                ScenarioCommand.MoveForward,
                ScenarioCommand.MoveForward,
                ScenarioCommand.MoveBackward
            ]);

        Assert.Equal(3, result.Steps.Count);

        Assert.Equal(new Position(1, 0), result.Steps[0].Position);
        Assert.Equal(Direction.South, result.Steps[0].Direction);

        Assert.Equal(new Position(2, 0), result.Steps[1].Position);
        Assert.Equal(Direction.South, result.Steps[1].Direction);

        Assert.Equal(new Position(1, 0), result.Steps[2].Position);
        Assert.Equal(Direction.South, result.Steps[2].Direction);
        Assert.Equal(Direction.South, result.FinalDirection);
    }

    [Fact]
    public void MoveOutOfBounds_RejectsMovePreservesLastPositionAndDirectionAndStopsSimulation()
    {
        ForestBoard board = CreateEmptyBoard(3);

        SimulationResult backwardAtStart = _engine.Run(
            board,
            [
                ScenarioCommand.MoveBackward,
                ScenarioCommand.MoveForward
            ]);

        Assert.Equal(SimulationStatus.OutOfBounds, backwardAtStart.Status);
        Assert.Single(backwardAtStart.Steps);
        Assert.Equal(new Position(0, 0), backwardAtStart.FinalPosition);
        Assert.Equal(Direction.South, backwardAtStart.FinalDirection);

        SimulationResult forwardPastBottom = _engine.Run(
            board,
            [
                ScenarioCommand.MoveForward,
                ScenarioCommand.MoveForward,
                ScenarioCommand.MoveForward,
                ScenarioCommand.TurnLeft
            ]);

        Assert.Equal(SimulationStatus.OutOfBounds, forwardPastBottom.Status);
        Assert.Equal(3, forwardPastBottom.Steps.Count);
        Assert.Equal(new Position(2, 0), forwardPastBottom.FinalPosition);
        Assert.Equal(Direction.South, forwardPastBottom.FinalDirection);
    }

    private static ForestBoard CreateEmptyBoard(int size) =>
        new(size, new ObstacleType[size, size]);
}
