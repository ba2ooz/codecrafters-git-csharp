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
// header                   -> 4 Bytes {PACK} | 4 Bytes version | 4 Bytes number of objects in the pack 
// non deltified obj-entry  -> n Bytes (3b type, (n-1)*7+4b length)  | compressed data 
//       OBJ_COMMIT         -> [MSB]001xxxx...
//       OBJ_TREE           -> [MSB]010xxxx...
//       OBJ_BLOB           -> [MSB]011xxxx...
//       OBJ_TAG            -> [MSB]100xxxx...
//
//     deltified obj-entry  -> nB (3b type, (n-1)*7+4b length)  | base object hash or offset                        | compressed data
//       OBJ_REF_DELTA      -> [MSB]111xxxx...                  | base object hash                                  | compressed data
//       OBJ_OFS_DELTA      -> [MSB]110xxxx...                  | negative offset from delta obj position in pack   | compressed data
//
// if [MSB] = 1 read next B as part of the same number until there is a B with
//    [MSB] = 0 this is last byte of the number       

// [Example] => [10000001 10000010 01111111] => 1111111 0000010 0000001 => 2081025 bits length
// 
// REF_DELTA Object structure:
// [A][B][C] 
// A - variable sequence of bytes that represent the size of uncompressed delta object
// B - strict sequence of bytes - precisely 20 - representing the hash of the object the current delta is referencing
// C - compressed data containing copy/insert instructions 
//
// OFS_DELTA Object structure:
// [A][D][C]
// A - the same as REF_DELTA
// D - the negative offset from the current delta object's header position (more precisely from the position of the current object first header byte - the one that contains the object type) in the pack file
// C - same as REF_DELTA
//
// C - Compressed delta object - Structure:
// [SVL][TVL][CIS]
// SVL - sequence of bytes representing the length of the source object (similar to A, only without a "object type" sequence of bits in the first byte)
// TVL - same as SVL only for the target object (the output buffer that will contain the result of copy/insert)
// CIS - copy/insert instructions sequence
//
// CIS structure:
// MSB tells whether this is a copy or insert instruction
// 0 - insert
// 1 - copy
// [0xxx xxxx] - this byte tells the size of bytes to copy, starting with the next one, from the delta to the output
// [1xxx xxxx] - this byte tells which 'offset' and 'size' bytes are present after itself
//i-7654 3210 
// 0 - offset1
// 1 - offset2
// 2 - offset3
// 3 - offset4
// 4 - size1
// 5 - size2
// 6 - size3
// 7 - MSB - indicates it is a copy instruction
//[1111 1111][offset1][offset2][offset3][offset4][size1][size2][size3]
//[1010 0100][offset3][size2]
//[1001 0000][size1]
// the offsets represent the starting read position in the base object
// the size represent the number of bytes to copy from the base object to the output
//
      