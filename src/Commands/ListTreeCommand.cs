using codecrafters_git.Abstractions;
using codecrafters_git.Objects;
using codecrafters_git.Records;

namespace codecrafters_git.Commands;

public class ListTreeCommand(IRepo repo) : ICommand
{
    public void Run(string[] args)
    {
        var flag = args[1];
        var hash = args[2];
        
        var treeEntries = GitTreeReader.ReadTreeEntries(repo, hash);
        PrintTreeEntries(treeEntries, flag == "--name-only");
    }
    
    private void PrintTreeEntries(SortedSet<TreeEntry> entries, bool nameOnly = false)
    {
        if (nameOnly)
        {
            foreach (var entry in entries)
                Console.WriteLine(entry.Name);
            
            return;
        }
        
        foreach (var entry in entries)
            Console.WriteLine($"{entry.FormattedType} {entry.Hash}\t{entry.Name}");
    }
}