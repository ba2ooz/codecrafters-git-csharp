using System.Text;

namespace codecrafters_git.Commands;

public class WriteCommitCommand : ICommand
{
    public void Run(string[] args)
    {
        // tree <last tree hash>
        // parent <parent tree hash>
        // author <name> <<email>> <unix time utc offset>
        // committer <name> <<email>> <unix time utc offset>
        // <blank line>
        // <commit message>

        var treeHash = args[1];
        var parentHash = args[3];
        var commitMessage = args[5];
        
        // if (!CheckPath(treeHash) || !CheckPath(parentHash))
        //     throw new ArgumentException("Invalid tree/parent hash");
        
        var authorName = "Jhon";
        var authorEmail = "jhon@doe.org";
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        string commitTree = $"tree {treeHash}{Environment.NewLine}" +
                            $"parent {parentHash}{Environment.NewLine}" +
                            $"author {authorName} <{authorEmail}> {unixTime}{Environment.NewLine}" +
                            $"committer {authorName} <{authorEmail}> {unixTime}{Environment.NewLine}{Environment.NewLine}" +
                            $"{commitMessage}{Environment.NewLine}";
        
        var commitHeader = $"commit {commitTree.Length}";

        var commit = new List<byte>();
        commit.AddRange(Encoding.ASCII.GetBytes(commitHeader));
        commit.Add(0);
        commit.AddRange(Encoding.ASCII.GetBytes(commitTree));
        
        var commitHash = HashObjectCommand.CompressObject(commit.ToArray());

        Console.WriteLine(commitHash);
    }

    private bool CheckPath(string pathHash)
    {
        return Directory.Exists(pathHash[..2]) && File.Exists(pathHash[2..]);
    }
    
}