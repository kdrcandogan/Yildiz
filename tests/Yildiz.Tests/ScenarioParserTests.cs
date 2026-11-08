using Yildiz.Core.Commands;

namespace Yildiz.Tests;

public sealed class ScenarioParserTests
{
    private readonly ScenarioParser _parser = new();

    [Fact]
    public void Parse_ValidMixedCommandsWithWhitespace_ReturnsExpectedCommandSequence()
    {
        ScenarioParseResult result = _parser.Parse(" N , P , R , L , J , N , İ , P ");

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorCommandNumber);
        Assert.Equal(
            [
                ScenarioCommand.MoveForward,
                ScenarioCommand.MoveBackward,
                ScenarioCommand.TurnRight,
                ScenarioCommand.TurnLeft,
                ScenarioCommand.Jump,
                ScenarioCommand.MoveForward,
                ScenarioCommand.Duck,
                ScenarioCommand.MoveBackward
            ],
            result.Commands);
    }

    [Fact]
    public void Parse_LowercaseAndUppercaseTokens_AreSupported()
    {
        ScenarioParseResult result = _parser.Parse("n,p,r,l,j,n,N,P,R,L,J,P");

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Commands.Count);
    }

    [Theory]
    [InlineData("İ,N")]
    [InlineData("i,N")]
    [InlineData("I,N")]
    [InlineData("ı,N")]
    [InlineData("i\u0307,N")]
    public void Parse_AllTurkishAndAsciiDuckVariations_ParseAsDuckCommand(string input)
    {
        ScenarioParseResult result = _parser.Parse(input);

        Assert.True(result.IsSuccess);
        Assert.Equal([ScenarioCommand.Duck, ScenarioCommand.MoveForward], result.Commands);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespaceInput_ReturnsFailure(string? input)
    {
        ScenarioParseResult result = _parser.Parse(input);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Commands);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData("N,,R", 2)]
    [InlineData(",N,R", 1)]
    [InlineData("N,R,", 3)]
    [InlineData("N,   ,R", 2)]
    public void Parse_WithEmptyToken_ReturnsFailureWithExactCommandNumber(string input, int expectedCommandNumber)
    {
        ScenarioParseResult result = _parser.Parse(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCommandNumber, result.ErrorCommandNumber);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData("N,X,R", 2, "X")]
    [InlineData("A,N", 1, "A")]
    [InlineData("N,NN,R", 2, "NN")]
    [InlineData("N,1,R", 2, "1")]
    public void Parse_WithInvalidToken_ReturnsFailureWithCommandNumberAndToken(
        string input,
        int expectedCommandNumber,
        string expectedToken)
    {
        ScenarioParseResult result = _parser.Parse(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCommandNumber, result.ErrorCommandNumber);
        Assert.Equal(expectedToken, result.InvalidToken);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData("J", 1)]
    [InlineData("İ", 1)]
    [InlineData("N,N,J", 3)]
    [InlineData("N,L,İ", 3)]
    public void Parse_WhenEndingWithJumpOrDuck_ReturnsFailureBeforeSimulation(
        string input,
        int expectedCommandNumber)
    {
        ScenarioParseResult result = _parser.Parse(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCommandNumber, result.ErrorCommandNumber);
        Assert.Empty(result.Commands);
    }

    [Theory]
    [InlineData("J,L,N", 2)]
    [InlineData("J,R,N", 2)]
    [InlineData("J,İ,N", 2)]
    [InlineData("J,J,N", 2)]
    [InlineData("İ,L,N", 2)]
    [InlineData("İ,R,N", 2)]
    [InlineData("İ,R,P", 2)]
    [InlineData("İ,J,N", 2)]
    [InlineData("N,J,L,N", 3)]
    public void Parse_WhenJumpOrDuckNotFollowedByMoveCommand_ReturnsFailure(
        string input,
        int expectedErrorCommandNumber)
    {
        ScenarioParseResult result = _parser.Parse(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedErrorCommandNumber, result.ErrorCommandNumber);
        Assert.Empty(result.Commands);
    }

    [Fact]
    public void Success_CommandsCollection_IsReadOnlyAndCannotBeMutatedExternally()
    {
        List<ScenarioCommand> source = [ScenarioCommand.MoveForward, ScenarioCommand.TurnLeft];
        ScenarioParseResult result = ScenarioParseResult.Success(source);

        source[0] = ScenarioCommand.TurnRight;

        Assert.Equal(ScenarioCommand.MoveForward, result.Commands[0]);

        IList<ScenarioCommand> asList = Assert.IsAssignableFrom<IList<ScenarioCommand>>(result.Commands);
        Assert.True(asList.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => asList[0] = ScenarioCommand.Jump);
    }
}
