namespace codecrafters_git.Abstractions;

public interface IRepo
{
    string RootDir { get; }
    string GitDir { get; }
    string ObjectsDir { get; }
    string RefsDir { get; }
    string Head { get; }

    string GetLooseObjectPath(string sha1Hex);
}