using codecrafters_git.Abstractions;
using codecrafters_git.Objects;

namespace codecrafters_git.Commands;

public class CheckoutCommand(IRepo repo) : ICommand
{
    public void Run(string[] args)
    {
        // throw new NotImplementedException();
    }
    
    public void Checkout(string hash)
    {
        CheckoutTree(repo.RootDir, hash);
    }
    
    private void CheckoutTree(string targetDir, string hash)
    {
        var treeEntries = GitTreeReader.ReadTreeEntries(repo, hash);
        
        foreach (var entry in treeEntries)
        {
            var targetPath = Path.Combine(targetDir, entry.Name);
            
            if (GitObjectMode.IsTree(entry.Type))
            {
                CheckoutTree(targetPath, entry.Hash);
            }
            else if (GitObjectMode.IsBlob(entry.Type))
            {
                var content = GitObjectIO.ReadObject(repo, entry.Hash)
                    .Split('\0')[1];    // get the content part
                
                Directory.CreateDirectory(targetDir);
                File.WriteAllText(targetPath, content);
            }
        }
    }
}