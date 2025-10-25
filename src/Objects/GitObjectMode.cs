namespace codecrafters_git.Objects;

public static class GitObjectMode
{
    public const string Tree = "40000";
    public const string NormalFile = "100644";
    public const string ExecutableFile = "100755";
    public const string SymbolicLink = "120000";
    public const string TreeHeader = "040000 tree";
    public static string BlobHeader(string blobType) => $"{blobType} blob";
    
    public static string GetObjectType(string mode) => mode switch
    {
        Tree => TreeHeader,
        NormalFile or ExecutableFile or SymbolicLink => BlobHeader(mode),
        _ => throw new InvalidOperationException($"Unknown mode {mode}")
    };
    
    public static bool IsTree(string mode) => mode is Tree or TreeHeader;
    public static bool IsBlob(string mode) => mode is NormalFile or ExecutableFile or SymbolicLink || mode == BlobHeader(mode);
}