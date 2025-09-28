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
        var blobContent = $"blob {fileContent.Length}\0{fileContent}";
        var blobContentByte = Encoding.UTF8.GetBytes(blobContent);
        var blobHash = SHA1.Create().ComputeHash(blobContentByte);
        var hashHex = BitConverter.ToString(blobHash).Replace("-", "").ToLowerInvariant();

        if (flag == "-w")
        {
            var dirToWriteTo = $".git/objects/{hashHex[..2]}";
            var fileToWriteTo = $"{dirToWriteTo}/{hashHex[2..]}";
            if (!Directory.Exists(dirToWriteTo))
                Directory.CreateDirectory(dirToWriteTo);
                
            using var blobStream = new MemoryStream(blobContentByte);
            using var fileWriter = File.OpenWrite(fileToWriteTo);
            using var compressedBlobContent = new ZLibStream(fileWriter, CompressionMode.Compress);
            blobStream.CopyTo(compressedBlobContent);
        }
        
        Console.WriteLine(hashHex);
        // Console.WriteLine(hashHex.Length);
    }
}