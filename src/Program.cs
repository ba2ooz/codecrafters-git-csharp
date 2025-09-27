using System;
using System.IO;
using System.IO.Compression;

if (args.Length < 1)
{
    Console.WriteLine("Please provide a command.");
    return;
}

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.Error.WriteLine("Logs from your program will appear here!");

string command = args[0];

if (command == "init")
{
    // Uncomment this block to pass the first stage
    
    Directory.CreateDirectory(".git");
    Directory.CreateDirectory(".git/objects");
    Directory.CreateDirectory(".git/refs");
    File.WriteAllText(".git/HEAD", "ref: refs/heads/main\n");
    Console.WriteLine("Initialized git directory");
}
else if (command == "cat-file")
{
    if (args.Length < 3)
    {
        Console.WriteLine("Command invalid.");
        return;
    }
 
    string commandFlag = args[1];
    if (commandFlag != "-p")
    {
        Console.WriteLine($"Unknown flag {commandFlag}");
        return;
    }

    string filehash = args[2];
    if (filehash.Trim() == "")
    {
        Console.WriteLine($"Provide a file hash");
        return;
    }

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
else
{
    throw new ArgumentException($"Unknown command {command}");
}