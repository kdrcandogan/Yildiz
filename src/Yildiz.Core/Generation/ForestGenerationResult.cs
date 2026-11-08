using Yildiz.Core.Models;

namespace Yildiz.Core.Generation;

public sealed record ForestGenerationResult
{
    public ForestGenerationResult(ForestBoard board, IEnumerable<Position> guaranteedPath)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(guaranteedPath);

        Board = board;
        GuaranteedPath = Array.AsReadOnly(guaranteedPath.ToArray());
    }

    public ForestBoard Board { get; }

    public IReadOnlyList<Position> GuaranteedPath { get; }
}
