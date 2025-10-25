using codecrafters_git.Objects;

namespace codecrafters_git.Records;

public record TreeEntry(string Type, string Hash, string Name) : IComparable<TreeEntry>
{
    public string FormattedType => GitObjectMode.GetObjectType(Type);
    public int CompareTo(TreeEntry? entry) => 
        string.Compare(Name, entry?.Name, StringComparison.OrdinalIgnoreCase);
}; 
