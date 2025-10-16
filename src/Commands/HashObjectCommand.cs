using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace codecrafters_git.Commands;

public class HashObjectCommand : ICommand
{
    public void Run(string[] args)
    {
        // read file content
        // prepare content to hash: "blob <content size in bytes>\0<file content>"
        // apply sha1 hash for above
        // compress and save if -w flag is provided
        // return 40 characters long sha1 hash
        
        // args -> hash-object -w <file-path>
        var flag = args[1];
        var filePath = args[2];
        
        var fileContent = File.ReadAllText(filePath);
        var rawBlobContent = GetRawBlob(fileContent); 
        var hashHex = CompressObject(rawBlobContent, flag == "-w");

        Console.WriteLine(hashHex);
    }

    public static byte[] GetRawBlob(string content)
    { 
        var blobContent = $"blob {content.Length}\0{content}";
        var blobContentByte = Encoding.UTF8.GetBytes(blobContent);
        return blobContentByte;
    }
    
    public static string CompressObject(byte[] payload, bool save = true)
    {
        var objectHash = SHA1.HashData(payload);
        var hashHex = Convert.ToHexStringLower(objectHash);

        if (!save) 
            return hashHex;
        
        var writeToPath = EnsurePath(hashHex);
        using var fileWriter = File.OpenWrite(writeToPath);
        using var compressedBlobContent = new ZLibStream(fileWriter, CompressionMode.Compress);
        using var blobStream = new MemoryStream(payload);
        blobStream.CopyTo(compressedBlobContent);

        return hashHex;
    }
    
    public static string EnsurePath(string hash)
    {
        var dirToWriteTo = $"{RepoInfo.RootDirectory}/.git/objects/{hash[..2]}";
        var fileToWriteTo = $"{dirToWriteTo}/{hash[2..]}";
        if (!Directory.Exists(dirToWriteTo))
            Directory.CreateDirectory(dirToWriteTo);
        
        return fileToWriteTo;
    }
}