using codecrafters_git.Abstractions;

namespace codecrafters_git.Infrastructure;

public class Repo : IRepo
{    
    public string RootDir { get; }
    public string GitDir { get; }
    public string ObjectsDir { get; }
    public string RefsDir { get; }
    public string Head { get; }
    
    
    public Repo(string? rootDir = null)
    {
        RootDir = string.IsNullOrWhiteSpace(rootDir) || rootDir == "." 
            ? Directory.GetCurrentDirectory() : rootDir;
        
        GitDir = Path.Combine(RootDir, ".git");
        ObjectsDir = Path.Combine(GitDir, "objects");
        RefsDir = Path.Combine(GitDir, "refs");
        Head = Path.Combine(GitDir, "HEAD");
    }
    
    public string GetLooseObjectPath(string sha1Hex)
    {
        return Path.Combine(ObjectsDir, sha1Hex[..2], sha1Hex[2..]);
    }
}