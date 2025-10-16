namespace codecrafters_git.Commands;

public class InitCommand : ICommand
{
    public void Run(string[] args)
    {
        Directory.CreateDirectory($"{RepoInfo.RootDirectory}/.git");
        Directory.CreateDirectory($"{RepoInfo.RootDirectory}/.git/objects");
        Directory.CreateDirectory($"{RepoInfo.RootDirectory}/.git/refs");
        File.WriteAllText($"{RepoInfo.RootDirectory}/.git/HEAD", "ref: refs/heads/main\n");
        Console.WriteLine("Initialized git directory");
    }
}