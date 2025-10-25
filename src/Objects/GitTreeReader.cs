using System.Text;
using codecrafters_git.Abstractions;
using codecrafters_git.Records;

namespace codecrafters_git.Objects;

public static class GitTreeReader
{
    // tree <size>\0
    //     <mode> <name>\0<20_byte_sha>
    //     <mode> <name>\0<20_byte_sha>
    
    public static SortedSet<TreeEntry> ReadTreeEntries(IRepo repo, string hash)
    {
        var entriesSet = new SortedSet<TreeEntry>();
        using var binaryReader = GitObjectIO.GetObjectReader(repo, hash);
        SkipHeader(binaryReader);
        
        while (TryReadStreamTreeEntry(binaryReader, out var treeEntry))
            entriesSet.Add(treeEntry!);
        
        return entriesSet;
    }

    private static bool TryReadStreamTreeEntry(BinaryReader br, out TreeEntry? entry)
    {
        try
        {
            entry = ReadTreeEntry(br);
            return true;
        }
        catch (EndOfStreamException)
        {
            entry = null;
            return false;
        }
    }

    private static TreeEntry ReadTreeEntry(BinaryReader br)
    {
        var type = ReadUntil(br, Constants.SpaceByte);
        var name = ReadUntil(br, Constants.NullByte);
        var hash = ReadHash(br);

        return new TreeEntry(type, hash, name);
    }

    private static void SkipHeader(BinaryReader br)
    {
        while (br.ReadByte() != Constants.NullByte);
    }

    private static string ReadUntil(BinaryReader br, byte delimiter)
    {
        byte b;
        var bytesList = new List<byte>();   
        while ((b = br.ReadByte()) != delimiter)
            bytesList.Add(b);
        
        return Encoding.ASCII.GetString(bytesList.ToArray());
    }

    private static string ReadHash(BinaryReader br)
    {
        var hashBytes = br.ReadBytes(Constants.HashLength);
        var hashHex = Convert.ToHexStringLower(hashBytes);
        return hashHex;
    }
}