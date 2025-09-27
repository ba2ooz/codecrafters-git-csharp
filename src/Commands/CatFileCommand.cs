namespace codecrafters_git.Commands;

using System.IO.Compression; 
    
public class CatFileCommand : ICommand
{
    public void Run(string[] args)
    {
        if (!IsValid(args))
            return;
        
        var filehash = args[2];
        var fileBlobDir = filehash[..2];
        var fileBlobName = filehash[2..];
        var fileBlobPath = $".git/objects/{fileBlobDir}/{fileBlobName}";
        if (!File.Exists(fileBlobPath))
        {
            Console.WriteLine("File not found: " + fileBlobPath);
            return;
        }

        using var fs = File.OpenRead(fileBlobPath);
        using var zlib = new ZLibStream(fs, CompressionMode.Decompress);
        using var reader = new StreamReader(zlib);
        var blobContent = reader.ReadToEnd()
            .Split('\0')    // skip the blob header part
            [1];    // get the content part
  
        Console.Write(blobContent);
    }

    public bool IsValid(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Command invalid.");
            return false;
        }
 
        string commandFlag = args[1];
        if (commandFlag != "-p")
        {
            Console.WriteLine($"Unknown flag {commandFlag}");
            return false;
        }

        string filehash = args[2];
        if (filehash.Trim() == "")
        {
            Console.WriteLine("Provide a file hash");
            return false;
        }

        return true;
    }
}