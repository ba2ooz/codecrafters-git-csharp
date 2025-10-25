using codecrafters_git.Abstractions;
using codecrafters_git.Objects;

namespace codecrafters_git.Commands;

public class WriteCommitCommand(IRepo repo) : ICommand
{
    public void Run(string[] args)
    {
        var treeHash = args[1];
        var parentHash = args[3];
        var commitMessage = args[5];
        
        var commitTree = ComposeCommit(treeHash, parentHash, commitMessage);
        var rawCommit = GitObjectIO.CreateCommitPayload(commitTree);
        var commitHash = GitObjectIO.WriteObject(repo, rawCommit);
        Console.WriteLine(commitHash);
    }

    private string ComposeCommit(string treeHash, string parentHash, string message)
    {
        // fake author data, getting the real author data is not relevant for this project 
        const string authorName = "John";
        const string authorEmail = "jhon@doe.org";
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var commitTree =  $"tree {treeHash}\n" +
                                $"parent {parentHash}\n" +
                                $"author {authorName} <{authorEmail}> {unixTime}\n" +
                                $"committer {authorName} <{authorEmail}> {unixTime}\n\n" +
                                $"{message}\n";
        return commitTree;
    }
}