namespace codecrafters_git.Commands;

public class InitCommand : ICommand
{
    public void Run(string[] args)
    {
        Directory.CreateDirectory(".git");
        Directory.CreateDirectory(".git/objects");
        Directory.CreateDirectory(".git/refs");
        File.WriteAllText(".git/HEAD", "ref: refs/heads/main\n");
        Console.WriteLine("Initialized git directory");
    }
}