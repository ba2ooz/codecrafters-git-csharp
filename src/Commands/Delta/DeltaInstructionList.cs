using System.Collections;
using codecrafters_git.Commands.Delta.Records;

namespace codecrafters_git.Commands.Delta;

public class DeltaInstructionList : IEnumerable<DeltaInstruction>
{
    private readonly List<DeltaInstruction> _instructions = [];

    public void AddCopy(int offset, int size) => _instructions.Add(new CopyInstruction(offset, size));
    public void AddInsert(byte[] data) => _instructions.Add(new InsertInstruction(data));

    public IEnumerator<DeltaInstruction> GetEnumerator() => _instructions.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
