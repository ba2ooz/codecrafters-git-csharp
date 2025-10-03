using System.Text;

namespace codecrafters_git.Commands;

public class WriteTreeCommand : ICommand
{
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

        var treeEntries = new List<byte>();
        foreach (var dir in  Directory.GetDirectories(directory))
        {
            var treeBytes = MakeTree(dir); 
            if (treeBytes.Length == 0)
                continue;
            
            var treeHash = HashObjectCommand.CompressObject(treeBytes); 
            var treeEntry = MakeEntry(treeHash, Path.GetFileName(dir));
            treeEntries.AddRange(treeEntry);
        }

        // add blob entries
        var blobEntries = GetTreeBlobs(directory);
        treeEntries.AddRange(blobEntries);

        // if directory is empty, return
        if (treeEntries.Count == 0)
            return [];
        
        // prepend the header
        var treeHeader = MakeHeader(treeEntries.Count);
        treeHeader.AddRange(treeEntries);
        treeEntries = treeHeader;
        
        return treeEntries.ToArray();
    }

    private List<byte> MakeHeader(int contentSize)
    {
        var headerBytes = new List<byte>();
        headerBytes.AddRange(Encoding.ASCII.GetBytes($"tree {contentSize.ToString()}"));
        headerBytes.Add(0);
        
        return headerBytes;
    }

    private List<byte> MakeEntry(string entryHash, string entryName)
    {
        var entryBytes = new List<byte>();
        entryBytes.AddRange(Encoding.ASCII.GetBytes($"{GetEntryType(entryName)}"));
        entryBytes.Add(0);
        entryBytes.AddRange(Convert.FromHexString(entryHash));

        return entryBytes;
    }

    private List<byte> GetTreeBlobs(string directory)
    {
        var blobEntries = new List<byte>();
        foreach (var file in Directory.GetFiles(directory))
        {
            Console.WriteLine("Processing File: " + file);
            
            var fileBlobContent = HashObjectCommand.GetBlobPayload(file);
            var fileHash = HashObjectCommand.CompressObject(fileBlobContent);
            var blobEntry = MakeEntry(fileHash, Path.GetFileName(file));
            blobEntries.AddRange(blobEntry);
        }
        
        return  blobEntries;
    }

    private string GetEntryType(string entryName) => entryName.Contains('.') switch
    {
        false => $"40000 {entryName}",
        true => $"100644 {entryName}",
    };
}