namespace Yildiz.Core.Models;

public readonly record struct Position(int Row, int Column)
{
    public Position Move(Direction direction, int steps = 1) => direction switch
    {
        Direction.North => new Position(Row - steps, Column),
        Direction.East => new Position(Row, Column + steps),
        Direction.South => new Position(Row + steps, Column),
        Direction.West => new Position(Row, Column - steps),
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction value.")
    };

    public bool IsAdjacentTo(Position other)
    {
        int rowDistance = Math.Abs(Row - other.Row);
        int columnDistance = Math.Abs(Column - other.Column);
        return rowDistance + columnDistance == 1;
    }
}
