using codecrafters_git.Abstractions;
using codecrafters_git.Objects;

namespace codecrafters_git.Commands;

public class CatFileCommand(IRepo repo) : ICommand
{
    public void Run(string[] args)
    {
        if (!IsValid(args))
            return;
        
        var fileHash = args[2];

        var blobObject = GitObjectIO.ReadObject(repo, fileHash);
        var blobContent = blobObject.Split('\0')[1]; // skip the blob header part and get the content part
        Console.Write(blobContent);
    }

    private bool IsValid(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Command invalid.");
            return false;
        }
 
        var commandFlag = args[1];
        if (commandFlag != "-p")
        {
            Console.WriteLine($"Unknown flag {commandFlag}");
            return false;
        }

        var fileHash = args[2];
        if (fileHash.Trim() == "")
        {
            Console.WriteLine("Provide a file hash");
            return false;
        }

        return true;
    }
}