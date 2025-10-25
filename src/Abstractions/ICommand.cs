namespace codecrafters_git.Abstractions;

public interface ICommand
{
    void Run(string[] args);
}