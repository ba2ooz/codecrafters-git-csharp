using codecrafters_git.Abstractions;
using codecrafters_git.Objects;
using codecrafters_git.Records;

namespace codecrafters_git.Commands;

public class WriteTreeCommand(IRepo repo) : ICommand
{
    public void Run(string[] args)
    {
        var currentTree = MakeTree(repo.RootDir);
        var treeHash = GitObjectIO.WriteObject(repo, currentTree);
        Console.WriteLine(treeHash);
    }

    private byte[] MakeTree(string directory)
    {
        if (directory == repo.GitDir) 
            return [];

        var childrenTrees = GetTreeChildrenTrees(directory);
        var childrenBlobs = GetTreeChildrenBlobs(directory);
        var children = childrenTrees.Concat(childrenBlobs);
        var sortedEntries = new SortedSet<TreeEntry>(children);
       
        // no entries, directory is empty, return
        if (sortedEntries.Count == 0)
            return [];

        return GitTreeWriter.CreateTreePayload(sortedEntries);
    }

    private List<TreeEntry> GetTreeChildrenTrees(string directory)
    {
        var treeEntries = new List<TreeEntry>();
        foreach (var dir in  Directory.GetDirectories(directory))
        {
            var treeBytes = MakeTree(dir);
            if (treeBytes.Length == 0) continue;
            var treeHash = GitObjectIO.WriteObject(repo, treeBytes); 
            var treeEntry = new TreeEntry(GitObjectMode.Tree, treeHash, Path.GetFileName(dir));
            treeEntries.Add(treeEntry);
        }
        
        return treeEntries;
    }
    
    private List<TreeEntry> GetTreeChildrenBlobs(string directory)
    {
        var blobEntries = new List<TreeEntry>();
        foreach (var file in Directory.GetFiles(directory))
        {
            var blobContent = File.ReadAllBytes(file);
            var rawBlobContent = GitObjectIO.CreateBlobPayload(blobContent); 
            var blobHash = GitObjectIO.WriteObject(repo, rawBlobContent);
            var blobEntry = new TreeEntry(GitObjectMode.NormalFile, blobHash, Path.GetFileName(file));
            blobEntries.Add(blobEntry);
        }
        
        return blobEntries;
    }
}