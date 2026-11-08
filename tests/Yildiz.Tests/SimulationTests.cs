using Yildiz.Core.Commands;
using Yildiz.Core.Generation;
using Yildiz.Core.Models;
using Yildiz.Core.Pathfinding;
using Yildiz.Core.Simulation;

namespace Yildiz.Tests;

public sealed class SimulationTests
{
    private readonly SimulationEngine _engine = new();
    private readonly ScenarioParser _parser = new();

    [Fact]
    public void Run_EmptyThreeByThreeBoardWithValidPath_ReachesHoleAndReturnsSuccess()
    {
        var board = new ForestBoard(3, new ObstacleType[3, 3]);
        ScenarioParseResult parseResult = _parser.Parse("N,N,L,N,N");

        SimulationResult result = _engine.Run(board, parseResult.Commands);

        Assert.Equal(SimulationStatus.Success, result.Status);
        Assert.Equal(5, result.Steps.Count);
        Assert.Equal(new Position(2, 2), result.FinalPosition);
        Assert.Equal(Direction.East, result.FinalDirection);
        Assert.True(result.FinalState.IsAlive);
    }

    [Fact]
    public void Run_WhenCommandsFinishBeforeReachingHole_ReturnsIncomplete()
    {
        var board = new ForestBoard(3, new ObstacleType[3, 3]);
        ScenarioParseResult parseResult = _parser.Parse("N,N");

        SimulationResult result = _engine.Run(board, parseResult.Commands);

        Assert.Equal(SimulationStatus.Incomplete, result.Status);
        Assert.Equal(2, result.Steps.Count);
        Assert.Equal(SimulationStatus.Running, result.Steps[0].Status);
        Assert.Equal(SimulationStatus.Incomplete, result.Steps[1].Status);
        Assert.Equal(new Position(2, 0), result.FinalPosition);
        Assert.Equal(Direction.South, result.FinalDirection);
    }

    [Fact]
    public void Run_WhenHoleReachedWithRemainingCommands_StopsImmediatelyAtSuccess()
    {
        var board = new ForestBoard(3, new ObstacleType[3, 3]);
        ScenarioParseResult parseResult = _parser.Parse("N,N,L,N,N,R,N,P");

        SimulationResult result = _engine.Run(board, parseResult.Commands);

        Assert.Equal(SimulationStatus.Success, result.Status);
        Assert.Equal(5, result.Steps.Count);
        Assert.Equal(new Position(2, 2), result.FinalPosition);
        Assert.Equal(Direction.East, result.FinalDirection);
    }

    [Fact]
    public void Run_SizeOneBoard_StartsImmediatelyInSuccessWithoutExecutingCommands()
    {
        var board = new ForestBoard(1, new ObstacleType[1, 1]);

        SimulationResult result = _engine.Run(board, [ScenarioCommand.MoveForward]);

        Assert.Equal(SimulationStatus.Success, result.Status);
        Assert.Empty(result.Steps);
        Assert.Equal(new Position(0, 0), result.FinalPosition);
        Assert.Equal(Direction.South, result.FinalDirection);
    }

    [Fact]
    public void Run_StepHistorySnapshots_AreImmutableAndNotMutatedBySubsequentSteps()
    {
        var board = new ForestBoard(3, new ObstacleType[3, 3]);
        ScenarioParseResult parseResult = _parser.Parse("J,N,L,N,N");

        SimulationResult result = _engine.Run(board, parseResult.Commands);

        Assert.Equal(5, result.Steps.Count);

        SimulationStep step1 = result.Steps[0];
        Assert.Equal(1, step1.StepNumber);
        Assert.Equal(ScenarioCommand.Jump, step1.Command);
        Assert.Equal(new Position(0, 0), step1.Position);
        Assert.Equal(Direction.South, step1.Direction);
        Assert.Equal(ObstacleType.Fence, step1.PendingBypass);
        Assert.Equal([new Position(0, 0)], step1.VisitedPositions);

        SimulationStep step2 = result.Steps[1];
        Assert.Equal(2, step2.StepNumber);
        Assert.Equal(ScenarioCommand.MoveForward, step2.Command);
        Assert.Equal(new Position(1, 0), step2.Position);
        Assert.Null(step2.PendingBypass);
        Assert.Equal([new Position(0, 0), new Position(1, 0)], step2.VisitedPositions);

        SimulationStep step5 = result.Steps[4];
        Assert.Equal(4, step5.VisitedPositions.Count);
        Assert.NotSame(step1.VisitedPositions, step2.VisitedPositions);
        Assert.NotSame(step2.VisitedPositions, step5.VisitedPositions);
    }

    [Fact]
    public void Run_TwoIndependentSimulationsOnSameBoard_ResetToInitialStateAndDoNotShareHistory()
    {
        ObstacleType[,] obstacles = new ObstacleType[3, 3];
        obstacles[1, 0] = ObstacleType.Wolf;
        var board = new ForestBoard(3, obstacles);

        SimulationResult failedRun = _engine.Run(board, [ScenarioCommand.MoveForward]);
        Assert.Equal(SimulationStatus.Dead, failedRun.Status);
        Assert.False(failedRun.FinalState.IsAlive);
        Assert.Equal(new Position(1, 0), failedRun.FinalPosition);

        ScenarioParseResult winningScenario = _parser.Parse("L,N,N,R,N,N");
        SimulationResult secondRun = _engine.Run(board, winningScenario.Commands);

        Assert.Equal(SimulationStatus.Success, secondRun.Status);
        Assert.True(secondRun.FinalState.IsAlive);
        Assert.Equal(new Position(0, 0), secondRun.Steps[0].Position);
        Assert.Equal(Direction.East, secondRun.Steps[0].Direction);
        Assert.Equal([new Position(0, 0)], secondRun.Steps[0].VisitedPositions);
        Assert.Equal(new Position(2, 2), secondRun.FinalPosition);
        Assert.Equal(ObstacleType.Wolf, board.GetObstacleAt(new Position(1, 0)));
    }

    [Theory]
    [InlineData(3, 42)]
    [InlineData(7, 42)]
    [InlineData(16, 42)]
    [InlineData(33, 42)]
    [InlineData(40, 42)]
    [InlineData(50, 42)]
    [InlineData(3, 7)]
    [InlineData(7, 99)]
    [InlineData(16, 2026)]
    public void Run_SampleSolutionAfterFailedScenario_UsesSameBoardResetsStartAndReachesSuccess(
        int size,
        int seed)
    {
        var pathBuilder = new GuaranteedPathBuilder();
        var generator = new ForestGenerator(pathBuilder);
        ForestGenerationResult generation = generator.Generate(size, seed);
        ForestBoard board = generation.Board;

        SimulationResult failedScenario = _engine.Run(board, [ScenarioCommand.MoveBackward]);
        Assert.Equal(SimulationStatus.OutOfBounds, failedScenario.Status);

        IReadOnlyList<ScenarioCommand> sampleCommands = pathBuilder.BuildCommands(
            generation.GuaranteedPath,
            board.InitialDirection);

        SimulationResult sampleRun = _engine.Run(board, sampleCommands);

        Assert.Equal(SimulationStatus.Success, sampleRun.Status);
        Assert.Equal(board.HolePosition, sampleRun.FinalPosition);
        Assert.True(sampleRun.FinalState.IsAlive);
        Assert.Equal([board.StartPosition], sampleRun.Steps[0].VisitedPositions.Take(1));
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("32", 32)]
    [InlineData("33", 33)]
    [InlineData("40", 40)]
    [InlineData("50", 50)]
    [InlineData("100", 100)]
    [InlineData("46340", 46340)]
    public void TryValidateForestSize_WithValidPositiveSizesIncludingAboveThirtyTwo_ReturnsTrue(
        string input,
        int expectedSize)
    {
        bool isValid = Yildiz.Console.ConsoleApplication.TryValidateForestSize(
            input,
            out int parsedSize,
            out string errorMessage);

        Assert.True(isValid);
        Assert.Equal(expectedSize, parsedSize);
        Assert.Equal(string.Empty, errorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-999999999999999")]
    [InlineData("46341")]
    [InlineData("999999999999999")]
    [InlineData("abc")]
    public void TryValidateForestSize_WithInvalidOrOverflowingInputs_ReturnsFalseWithControlledErrorMessage(
        string? invalidInput)
    {
        bool isValid = Yildiz.Console.ConsoleApplication.TryValidateForestSize(
            invalidInput,
            out int parsedSize,
            out string errorMessage);

        Assert.False(isValid);
        Assert.Equal(0, parsedSize);
        Assert.False(string.IsNullOrWhiteSpace(errorMessage));
    }

    [Theory]
    [InlineData(33)]
    [InlineData(40)]
    [InlineData(50)]
    [InlineData(60)]
    public void ForestConsoleRenderer_WithLargeBoards_RendersInitialForestAndSimulationWithoutErrors(int size)
    {
        var pathBuilder = new GuaranteedPathBuilder();
        var generator = new ForestGenerator(pathBuilder);
        ForestGenerationResult generation = generator.Generate(size, seed: 42);
        IReadOnlyList<ScenarioCommand> commands = pathBuilder.BuildCommands(
            generation.GuaranteedPath,
            generation.Board.InitialDirection);
        SimulationResult simulationResult = _engine.Run(generation.Board, commands);

        using var writer = new StringWriter();
        Spectre.Console.IAnsiConsole console = Spectre.Console.AnsiConsole.Create(
            new Spectre.Console.AnsiConsoleSettings
            {
                Ansi = Spectre.Console.AnsiSupport.No,
                ColorSystem = Spectre.Console.ColorSystemSupport.NoColors,
                Interactive = Spectre.Console.InteractionSupport.No,
                Out = new Spectre.Console.AnsiConsoleOutput(writer)
            });

        var renderer = new Yildiz.Console.Rendering.ForestConsoleRenderer(console);
        renderer.RenderInitialForest(generation.Board);
        renderer.RenderSimulation(generation.Board, simulationResult, commands.Count, TimeSpan.Zero);

        string output = writer.ToString();
        Assert.Contains($"Orman: {size}×{size}", output);
        Assert.Contains("TEBRİKLER! YILDIZ YUVASINA ULAŞTI.", output);
        Assert.Contains($"Son Konum: ({size}, {size})", output);
    }
}
