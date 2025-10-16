using System.Text;

namespace codecrafters_git.Commands;

public class WriteCommitCommand : ICommand
{
    public void Run(string[] args)
    {
        var treeHash = args[1];
        var parentHash = args[3];
        var commitMessage = args[5];
        
        // if (!CheckPath(treeHash) || !CheckPath(parentHash))
        //     throw new ArgumentException("Invalid tree/parent hash");
        
        var commitTree = ComposeCommit(treeHash, parentHash, commitMessage);
        var rawCommit = GetRawCommit(commitTree);
        var commitHash = HashObjectCommand.CompressObject(rawCommit);
        Console.WriteLine(commitHash);
    }

    public static byte[] GetRawCommit(string body)
    {
        var header = $"commit {body.Length}";
        var commit = new List<byte>();
        commit.AddRange(Encoding.ASCII.GetBytes(header));
        commit.Add(0);
        commit.AddRange(Encoding.ASCII.GetBytes(body));
        
        return commit.ToArray();
    }

    private string ComposeCommit(string treeHash, string parentHash, string message)
    {
        // some dummy author data, getting the real author data is not relevant for the purpose of this project 
        const string authorName = "Jhon";
        const string authorEmail = "jhon@doe.org";
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        // commit structure:
        //  tree <last tree hash>
        //  parent <parent tree hash>
        //  author <name> <<email>> <unix time utc offset>
        //  committer <name> <<email>> <unix time utc offset>
        //  <blank line>
        //  <commit message>
        var commitTree =  $"tree {treeHash}\n" +
                                $"parent {parentHash}\n" +
                                $"author {authorName} <{authorEmail}> {unixTime}\n" +
                                $"committer {authorName} <{authorEmail}> {unixTime}\n\n" +
                                $"{message}\n";
        return commitTree;
    }
    
    private bool CheckPath(string pathHash)
    {
        return Directory.Exists(pathHash[..2]) && File.Exists(pathHash[2..]);
    }
    
}