using System.Text;

namespace codecrafters_git.Commands;

public class WriteTreeCommand : ICommand
{
    private const string TreeType = "40000";
    private const string BlobType = "100644";
    
    public void Run(string[] args)
    {
        var stagingArea = Directory.GetCurrentDirectory();
        var currentTree = MakeTree(stagingArea);
        var treeHash = HashObjectCommand.CompressObject(currentTree);

        Console.WriteLine(treeHash);
    }

    private byte[] MakeTree(string directory)
    {
        if (Path.GetFileName(directory).StartsWith(".git"))
            return [];

        var childrenTrees = GetTreeChildrenTrees(directory);
        var childrenBlobs = GetTreeChildrenBlobs(directory);
        var children = childrenTrees.Concat(childrenBlobs);
        var sortedEntries = new SortedSet<TreeEntry>(children);
       
        // no entries, directory is empty, return
        if (sortedEntries.Count == 0)
            return [];

        var treeContent = GetRawTreeEntries(sortedEntries);
        var treePayload = GetRawTreePayload(treeContent);
        
        return treePayload;
    }

    private List<byte> GetRawTreeEntries(SortedSet<TreeEntry> treeEntries)
    {
        var content = new List<byte>();
        foreach (var entry in treeEntries)
            content.AddRange(GetRawEntry(entry));
        
        return content;
    }

    public static byte[] GetRawTreePayload(IEnumerable<byte> treeEntries)
    {
        var entries = treeEntries.ToList();
        var treeHeader = GetRawHeader(entries.Count);
        var tree = new List<byte>();
        tree.AddRange(treeHeader);
        tree.AddRange(entries);
        
        return tree.ToArray();
    }
    
    private static List<byte> GetRawHeader(int contentSize)
    {
        var headerSize = $"tree {contentSize}";
        var header = Encoding.ASCII.GetBytes(headerSize);
        
        var headerBytes = new List<byte>();
        headerBytes.AddRange(header);
        headerBytes.Add(0);
        
        return headerBytes;
    }
    
    private List<byte> GetRawEntry(TreeEntry treeEntry)
    {
        var entryModeName = $"{treeEntry.Type} {treeEntry.Name}";
        var byteModeName = Encoding.ASCII.GetBytes(entryModeName);
        var byteEntryHash = Convert.FromHexString(treeEntry.Hash);
        
        var entryBytes = new List<byte>();
        entryBytes.AddRange(byteModeName);
        entryBytes.Add(0);
        entryBytes.AddRange(byteEntryHash);

        return entryBytes;
    }

    private List<TreeEntry> GetTreeChildrenTrees(string directory)
    {
        var treeEntries = new List<TreeEntry>();
        foreach (var dir in  Directory.GetDirectories(directory))
        {
            var treeBytes = MakeTree(dir);
            if (treeBytes.Length == 0) continue;
            var treeHash = HashObjectCommand.CompressObject(treeBytes); 
            var treeEntry = new TreeEntry(TreeType, treeHash, Path.GetFileName(dir));
            treeEntries.Add(treeEntry);
        }
        
        return treeEntries;
    }
    
    private List<TreeEntry> GetTreeChildrenBlobs(string directory)
    {
        var blobEntries = new List<TreeEntry>();
        foreach (var file in Directory.GetFiles(directory))
        {
            var blobContent = File.ReadAllText(file);
            var rawBlobContent = HashObjectCommand.GetRawBlob(blobContent); 
            var blobHash = HashObjectCommand.CompressObject(rawBlobContent);
            var blobEntry = new TreeEntry(BlobType, blobHash, Path.GetFileName(file));
            blobEntries.Add(blobEntry);
        }
        
        return blobEntries;
    }
}