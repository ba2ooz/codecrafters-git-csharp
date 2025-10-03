namespace codecrafters_git;

public record TreeEntry(string Type, string Hash, string Name) : IComparable<TreeEntry>
{
    public int CompareTo(TreeEntry? entry) => 
        string.Compare(Name, entry?.Name, StringComparison.OrdinalIgnoreCase);
}; 
