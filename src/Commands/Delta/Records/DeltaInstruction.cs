namespace codecrafters_git.Commands.Delta.Records;

public abstract record DeltaInstruction();

public sealed record CopyInstruction(int offset, int size) : DeltaInstruction;
public sealed record InsertInstruction(byte[] data) : DeltaInstruction;