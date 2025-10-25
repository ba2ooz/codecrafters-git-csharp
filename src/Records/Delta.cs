using codecrafters_git.Delta;

namespace codecrafters_git.Records;

public record Delta()
{
    public int SourceLength { get; init;  }
    public int TargetLength { get; init; }
    public DeltaInstructionList Instructions { get; init;  } = new();
}