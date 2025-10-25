using codecrafters_git.Abstractions;

namespace codecrafters_git.Commands;

public class InitCommand(IRepo repo) : ICommand
{
    public void Run(string[] args)
    {
        Directory.CreateDirectory(repo.GitDir);
        Directory.CreateDirectory(repo.ObjectsDir);
        Directory.CreateDirectory(repo.RefsDir);
        File.WriteAllText(repo.Head, "ref: refs/heads/main\n");
        
        Console.WriteLine("Initialized git directory");
    }
}