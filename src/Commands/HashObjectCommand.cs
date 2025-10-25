using codecrafters_git.Abstractions;
using codecrafters_git.Objects;

namespace codecrafters_git.Commands;

public class HashObjectCommand(IRepo repo) : ICommand
{
    public void Run(string[] args)
    {
        // args -> hash-object -w <file-path>
        var flag = args[1];
        var filePath = args[2];
        
        var fileContent = File.ReadAllBytes(filePath);
        var rawBlobContent = GitObjectIO.CreateBlobPayload(fileContent);
        var hashHex = GitObjectIO.WriteObject(repo, rawBlobContent, flag == "-w");

        Console.WriteLine(hashHex);
    }
}