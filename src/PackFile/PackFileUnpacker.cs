using codecrafters_git.Abstractions;
using codecrafters_git.Delta;
using codecrafters_git.Objects;

namespace codecrafters_git.PackFile;

public class PackFileUnpacker(IRepo repo)
{
    private readonly PackfileReader _reader = new();
    private readonly DeltaResolver _deltaResolver = new(repo);
    
    // Track objects for delta resolution
    private readonly Dictionary<int, string> _positionToHash = new();
    private readonly Dictionary<string, PackObjectType> _hashToType = new();

    public string UnpackAll(byte[] pack)
    {
        var position = _reader.FindPackDataStart(pack);
        var endPosition = pack.Length - Constants.HashLength; // exclude the pack checksum
        var firstCommitHash = string.Empty;
        
        while (position < endPosition)
        {
            var objHash = UnpackObject(pack, ref position);
            // Track the first commit for checkout
            if (string.IsNullOrEmpty(firstCommitHash) && _hashToType[objHash] is PackObjectType.COMMIT)
                firstCommitHash = objHash;
        }
        
        return firstCommitHash;
    }
    
    private string UnpackObject(byte[] pack, ref int position)
    {
        var headerPosition = position;
        var (headerType, _) = _reader.ReadObjectHeader(pack, ref position);
        var actualType = headerType;
        var objectData = ProcessObject(pack, headerPosition, ref position, ref actualType);
        var objectHash = SaveObject(headerType, objectData);
        
        // Track object hash and type for delta resolution
        _positionToHash[headerPosition] = objectHash;
        _hashToType[objectHash] = actualType;
        
        return objectHash;
    }

    private byte[] ProcessObject(byte[] pack, int headerPosition, ref int position, ref PackObjectType type) 
        => type switch
        {
            PackObjectType.REF_DELTA => ProcessRefDeltaObject(pack, ref position, out type),
            PackObjectType.OFS_DELTA => ProcessOfsDeltaObject(pack, headerPosition, ref position, out type),
            _ => _reader.DecompressObject(pack, ref position, pack.Length - position - Constants.HashLength)
        };

    private byte[] ProcessRefDeltaObject(byte[] pack, ref int position, out PackObjectType actualType)
    {
        var baseObjectHash = _reader.ReadDeltaReferenceHash(pack, ref position);
        var deltaData = _reader.DecompressObject(pack, ref position, pack.Length - position - Constants.HashLength);
        actualType = _hashToType[baseObjectHash]; // get the type of the base object
        return _deltaResolver.ResolveDelta(baseObjectHash, actualType, deltaData);
    }

    private byte[] ProcessOfsDeltaObject(byte[] pack, int headerPosition, ref int position, out PackObjectType actualType)
    {
        var offset = _reader.ReadDeltaOffset(pack, ref position);
        var deltaData = _reader.DecompressObject(pack, ref position, pack.Length - position - Constants.HashLength);
        var baseObjectPosition = headerPosition - offset;
        var baseObjectHash = _positionToHash[baseObjectPosition];
        actualType = _hashToType[baseObjectHash];
        return _deltaResolver.ResolveDelta(baseObjectHash, actualType, deltaData);
    }
    
    private string SaveObject(PackObjectType type, byte[] data)
    {
        return type switch
        {
            PackObjectType.COMMIT => GitObjectIO.WriteObject(repo, GitObjectIO.CreateCommitPayload(data)),
            PackObjectType.TREE => GitObjectIO.WriteObject(repo, GitObjectIO.CreateTreePayload(data)),
            PackObjectType.BLOB => GitObjectIO.WriteObject(repo, GitObjectIO.CreateBlobPayload(data)),
            PackObjectType.REF_DELTA or 
            PackObjectType.OFS_DELTA => GitObjectIO.WriteObject(repo, data),
            PackObjectType.TAG => null, // skip tags
            _ => throw new NotSupportedException($"Unsupported object type: {type}")
        } ?? string.Empty;
    }
}