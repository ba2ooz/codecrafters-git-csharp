using System.Text;
using codecrafters_git.Records;

namespace codecrafters_git.Objects;

public class GitTreeWriter
{
    public static byte[] CreateTreePayload(SortedSet<TreeEntry> entries)
    {
        var entriesBytes = SerializeTreeEntries(entries);
        return GitObjectIO.CreateTreePayload(entriesBytes);
    }
    
    private static byte[] SerializeTreeEntries(SortedSet<TreeEntry> entries)
    {
        var content = new List<byte>();
        foreach (var entry in entries)
            content.AddRange(SerializeTreeEntry(entry));
        
        return content.ToArray();
    }
    
    private static byte[] SerializeTreeEntry(TreeEntry entry)
    {
        // Git tree format: <mode> <name>\0<20-byte-hash>
        var entryHeader = Encoding.ASCII.GetBytes($"{entry.Type} {entry.Name}\0");
        var hashBytes = Convert.FromHexString(entry.Hash);
        
        var entryPayload = new byte[entryHeader.Length + hashBytes.Length];
        Array.Copy(entryHeader, 0, entryPayload, 0, entryHeader.Length);
        Array.Copy(hashBytes, 0, entryPayload, entryHeader.Length, hashBytes.Length);
        return entryPayload;
    }
}