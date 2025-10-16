using System.IO.Compression;
using System.Text;
using codecrafters_git.Commands.Delta.Records;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace codecrafters_git.Commands.Delta;

public class DeltaResolver
{
    public byte[] ResolveDelta(string baseObject, PackObjectType baseType, byte[] delta)
    {
        var deltaObject = DeltaParser.Parse(delta);
        var targetObject = new byte[deltaObject.TargetLength];
        
        var rawBaseObject = GetRawBaseObject(baseObject, deltaObject.SourceLength);
        ExecuteInstructions(deltaObject.Instructions, rawBaseObject, ref targetObject);
        
        return GetRawTargetObjectByType(targetObject, baseType);
    }

    private byte[] GetRawTargetObjectByType(byte[] targetObject, PackObjectType baseType) => baseType switch
    {
        PackObjectType.COMMIT => WriteCommitCommand.GetRawCommit(Encoding.ASCII.GetString(targetObject)),
        PackObjectType.TREE => WriteTreeCommand.GetRawTreePayload(targetObject),
        PackObjectType.BLOB => HashObjectCommand.GetRawBlob(Encoding.ASCII.GetString(targetObject)),
        _ => throw new ArgumentOutOfRangeException(nameof(baseType))
    };

    private byte[] GetRawBaseObject(string baseObject, int size)
    {
        var fileBlobDir = baseObject[..2];
        var fileBlobName = baseObject[2..];
        var fileBlobPath = $"{RepoInfo.RootDirectory}/.git/objects/{fileBlobDir}/{fileBlobName}";
        using var fs = File.OpenRead(fileBlobPath);
        using var ins = new InflaterInputStream(fs);
        using var br = new BinaryReader(ins);
        while (br.ReadByte() != '\0') ;
        
        var uncompressedBaseObject = new byte[size];
        _ = br.Read(uncompressedBaseObject, 0, size);

        return uncompressedBaseObject;
    }

    private void ExecuteInstructions(DeltaInstructionList instructions, byte[] rawBaseObject, ref byte[] targetObject)
    {
        var targetOffset = 0;
        
        foreach (var instruction in instructions)
        {
            switch (instruction)
            {
                case CopyInstruction copy:
                {
                    Buffer.BlockCopy(rawBaseObject, copy.offset, targetObject, targetOffset,copy.size);
                    targetOffset += copy.size;
                    break;
                }
                case InsertInstruction insert:
                {
                    Buffer.BlockCopy(insert.data, 0, targetObject, targetOffset,insert.data.Length);
                    targetOffset += insert.data.Length;
                    break;
                }
            }
        }
    }
}
