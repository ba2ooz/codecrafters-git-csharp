namespace codecrafters_git.Commands.Delta.Records;

public record Delta()
{
    // public int Type { get; } = Type;
    // public int headerIndex { get; set; }
    public int SourceLength { get; init;  }
    public int TargetLength { get; init; }

    public string SourceObjectHash { get; set; }
    public DeltaInstructionList Instructions { get; init;  } = new();
}