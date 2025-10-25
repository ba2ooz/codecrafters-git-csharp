using codecrafters_git.Abstractions;

namespace codecrafters_git.Infrastructure;

public class RepoFactory : IRepoFactory
{
    public IRepo Create(string? rootDir = null) => new Repo(rootDir);
}