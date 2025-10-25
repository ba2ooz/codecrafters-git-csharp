namespace codecrafters_git.Objects;

public enum GitObjectType
{
    Blob,
    Tree,
    Commit
}

public enum PackObjectType
{
    COMMIT = 1,
    TREE = 2,
    BLOB = 3,
    TAG = 4,
    OFS_DELTA = 6,
    REF_DELTA = 7
}

public static class GitObjectTypeExtensions
{
    public static string ToGitString(this GitObjectType type) => type switch
    {
        GitObjectType.Blob => "blob",
        GitObjectType.Tree => "tree",
        GitObjectType.Commit => "commit",
        _ => throw new ArgumentException($"Unknown type: {type}")
    };
}