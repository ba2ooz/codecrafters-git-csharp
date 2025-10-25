using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using codecrafters_git.Abstractions;

namespace codecrafters_git.Objects;

public static class GitObjectIO
{
    
    // -----------------------
    // PAYLOAD CREATION OPERATIONS
    // -----------------------
    
    public static byte[] CreateObjectPayload(GitObjectType type, byte[] content)
    {
        var header = Encoding.ASCII.GetBytes($"{type.ToGitString()} {content.Length}\0");
        var payload = new byte[header.Length + content.Length];
        Array.Copy(header, 0, payload, 0, header.Length);
        Array.Copy(content, 0, payload, header.Length, content.Length);
        return payload;
    }
    
    public static byte[] CreateBlobPayload(byte[] content) => CreateObjectPayload(GitObjectType.Blob, content);
    public static byte[] CreateBlobPayload(string content) => CreateObjectPayload(GitObjectType.Blob, Encoding.UTF8.GetBytes(content));
    public static byte[] CreateCommitPayload(byte[] content) =>  CreateObjectPayload(GitObjectType.Commit, content);
    public static byte[] CreateCommitPayload(string content) => CreateObjectPayload(GitObjectType.Commit, Encoding.UTF8.GetBytes(content));
    public static byte[] CreateTreePayload(byte[] treeEntries) => CreateObjectPayload(GitObjectType.Tree, treeEntries);
    

    // -----------------------
    // READ OPERATIONS
    // -----------------------
    public static byte[] ReadObjectBytes(IRepo repo, string sha1Hash)
    {
        var filePath = repo.GetLooseObjectPath(sha1Hash);
        using var fs = File.OpenRead(filePath);
        using var zlib = new ZLibStream(fs, CompressionMode.Decompress);
        using var ms = new MemoryStream();
        zlib.CopyTo(ms);
        return ms.ToArray();
    }
    
    public static BinaryReader GetObjectReader(IRepo repo, string sha1Hash)
    {
        var bytes = ReadObjectBytes(repo, sha1Hash);
        return new BinaryReader(new MemoryStream(bytes));
    }
    
    public static string ReadObject(IRepo repo, string sha1Hex)
    {
        return ReadObject(repo, sha1Hex, DecompressWithZLib);
    }

    public static string ReadObject(IRepo repo, string sha1Hex, Func<Stream, string> decompress)
    {
        var filePath = repo.GetLooseObjectPath(sha1Hex);
        using var fs = File.OpenRead(filePath);
        return decompress(fs);
    }
    
    public static byte[] ReadObject(IRepo repo, string sha1Hex, Func<Stream, byte[]> decompress)
    {
        var filePath = repo.GetLooseObjectPath(sha1Hex);
        using var fs = File.OpenRead(filePath);
        return decompress(fs);
    }

    private static string DecompressWithZLib(Stream stream)
    {
        using var zlib = new ZLibStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(zlib);
        return reader. ReadToEnd();
    }
    
    // -----------------------
    // WRITE OPERATIONS
    // -----------------------
    
    public static string WriteObject(IRepo repo, byte[] payload, bool save = true)
    {
        return WriteObject(repo, payload, save, CompressWithZLib);
    }

    public static string WriteObject(IRepo repo, byte[] payload, bool save, Action<Stream, byte[]> compress)
    {
        var objectHash = SHA1.HashData(payload);
        var sha1Hex = Convert.ToHexStringLower(objectHash);

        if (!save)
            return sha1Hex;

        var writeToPath = repo.GetLooseObjectPath(sha1Hex);
        var dir = Path.GetDirectoryName(writeToPath);
        if (dir is null)
            throw new Exception("Could not get directory name");
        
        Directory.CreateDirectory(dir);   
        using var fileWriter = File.OpenWrite(writeToPath);
        compress(fileWriter, payload);

        return sha1Hex;
    }

    private static void CompressWithZLib(Stream output, byte[] payload)
    {
        using var compressor = new ZLibStream(output, CompressionMode.Compress);
        compressor.Write(payload, 0, payload.Length);
    }
}