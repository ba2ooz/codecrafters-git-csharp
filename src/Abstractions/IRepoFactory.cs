namespace codecrafters_git.Abstractions;

public interface IRepoFactory
{
    IRepo Create(string? rootDir = null);
}