using codecrafters_git.Abstractions;
using codecrafters_git.Objects;
using codecrafters_git.PackFile;
using codecrafters_git.Protocol;

namespace codecrafters_git.Commands;

public class CloneCommand(IRepoFactory repoFactory) : ICommand
{
    private IRepo? _repo;
    private readonly GitHttpClient _client = new(new HttpClient());
    
    public void Run(string[] args)
    {
        var gitUrl = $"{args[1]}";
        var localRepoDestination = args[2];

        RunAsync(gitUrl, localRepoDestination).Wait();
    }

    public async Task RunAsync(string repoUrl, string repoDestination)
    {
        // 1. Init new repo
        _repo = repoFactory.Create(repoDestination);
        new InitCommand(_repo).Run([]);
        
        // 2. Fetch refs from remote
        var refsData = await _client.GetRefAdvertismentAsync(repoUrl);
        var refs = GitProtocolParser.ParseRefs(refsData);
        
        // 3. Get HEAD branch hash
        var headRefHash = refs[1].Split(" HEAD")[0];
        
        // 4. Negotiate and download pack
        var negotiationPayload = GitProtocolParser.CreateNegotiationPayload(headRefHash);
        var pack = await _client.GetPackAsync(repoUrl, negotiationPayload);
        
        // 5. Unpack objects
        var commitHash = new PackFileUnpacker(_repo).UnpackAll(pack);
        
        // 6. Checkout working tree
        var commitContent = GitObjectIO.ReadObject(_repo, commitHash);
        var treeHash = ExtractTreeHash(commitContent);
        new CheckoutCommand(_repo).Checkout(treeHash);
        
        Console.WriteLine($"Cloned repository to {repoDestination}");
    }
    
    private string ExtractTreeHash(string commitContent)
    {
        return commitContent
            .Split($"{GitObjectType.Tree.ToGitString()} ")[1]
            .Split("\n")[0];
    }
}


// header, header followed by obj-entries
// header              -> 4B {PACK} | 4B version | 4B number of objects contained 
// non delta obj-entry -> nB (3b type, (n-1)*7+4b length)  | compressed data 
//       OBJ_COMMIT    -> [MSB]001xxxx...
//       OBJ_TREE      -> [MSB]010xxxx...
//       OBJ_BLOB      -> [MSB]011xxxx...
//       OBJ_TAG       -> [MSB]100xxxx...
//
//     delta obj-entry -> nB (3b type, (n-1)*7+4b length) | base obj-name or offset | compressed data
//       OBJ_REF_DELTA -> [MSB]111xxxx... | base obj-name | compressed data
//       OBJ_OFS_DELTA -> [MSB]110xxxx... | negative offset from delta obj position in pack | compressed data
//
// if [MSB] = 1 read next B as part of the same number until there is a B with
//    [MSB] = 0 this is last byte of the number        
// [Example] => [10000001 10000010 01111111] => 1111111 0000010 0000001 => 2081025 bits length
// 