using System.IO.Compression;
using System.Text;

namespace codecrafters_git.Commands;

public class ListTreeCommand : ICommand
{
    private record TreeEntry(string Type, string Hash, string Name) : IComparable<TreeEntry>
    {
        public int CompareTo(TreeEntry? entry) => 
            string.Compare(Name, entry?.Name, StringComparison.OrdinalIgnoreCase);
    }; 
    
    private const byte SpaceByte = (byte)' ';
    private const byte NullByte = 0;
    private const int ByteHashLength = 20;
   
    public void Run(string[] args)
    {
        // tree <size>\0
        //     <mode> <name>\0<20_byte_sha>
        //     <mode> <name>\0<20_byte_sha>
        
        var flag = args[1];
        var hash = args[2];
        
        var path = GetObjectPath(hash);
        var treeEntries = ReadStreamTreeEntries(path);
        PrintTreeEntries(treeEntries, flag == "--name-only");
    }

    private SortedSet<TreeEntry> ReadStreamTreeEntries(string objPath)
    {
        var entriesSet = new SortedSet<TreeEntry>();
        using var fs = File.OpenRead(objPath);
        using var zlib = new ZLibStream(fs, CompressionMode.Decompress);
        using var br = new BinaryReader(zlib);

        SkipHeader(br);
        while (TryReadStreamTreeEntry(br, out var treeEntry))
            entriesSet.Add(treeEntry!);
        
        return entriesSet;
    }

    private bool TryReadStreamTreeEntry(BinaryReader br, out TreeEntry? entry)
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

    private TreeEntry ReadTreeEntry(BinaryReader br)
    {
        var objectType = ReadUntil(br, SpaceByte);
        var objectName = ReadUntil(br, NullByte);
        var objectHash = ReadHash(br);

        return new TreeEntry(GetObjectType(objectType), objectHash, objectName);
    }

    private void SkipHeader(BinaryReader br)
    {
        while (br.ReadByte() != NullByte) ;
    }

    private string ReadUntil(BinaryReader br, byte delimiter)
    {
        byte b;
        var bytesList = new List<byte>();   
        while ((b = br.ReadByte()) != delimiter)
            bytesList.Add(b);
        
        return Encoding.ASCII.GetString(bytesList.ToArray());
    }

    private string ReadHash(BinaryReader br)
    {
        var hashBytes = br.ReadBytes(ByteHashLength);
        var hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return hashHex;
    }
    
    private string GetObjectPath(string objectHash)
    {
        var directory = objectHash[..2];
        var fileName = objectHash[2..];
        return $".git/objects/{directory}/{fileName}";
    }

    private string GetObjectType(string mode) => mode switch
    {
        "40000" => "040000 tree",
        "100644" or "100755" or "120000" => $"{mode} blob",
        _ => throw new InvalidOperationException($"Unknown mode {mode}")
    };
    
    private void PrintTreeEntries(SortedSet<TreeEntry> entries, bool nameOnly = false)
    {
        if (nameOnly)
        {
            foreach (var entry in entries)
                Console.WriteLine(entry.Name);
            
            return;
        }
        
        foreach (var entry in entries)
            Console.WriteLine($"{entry.Type} {entry.Hash}\t{entry.Name}");
    }
}