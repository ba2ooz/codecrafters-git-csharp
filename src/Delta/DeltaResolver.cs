using codecrafters_git.Abstractions;
using codecrafters_git.Objects;
using codecrafters_git.Records;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace codecrafters_git.Delta;

public class DeltaResolver(IRepo repo)
{
    public byte[] ResolveDelta(string baseObject, PackObjectType baseType, byte[] delta)
    {
        var deltaObject = DeltaParser.Parse(delta);
        var targetObject = new byte[deltaObject.TargetLength];
        
        var rawBaseObject = ReadBaseObject(baseObject, deltaObject.SourceLength);
        ExecuteInstructions(deltaObject.Instructions, rawBaseObject, ref targetObject);
        
        return CreateObjectPayload(targetObject, baseType);
    }

    private byte[] CreateObjectPayload(byte[] targetObject, PackObjectType baseType) 
        => baseType switch
        {
            PackObjectType.COMMIT => GitObjectIO.CreateCommitPayload(targetObject),
            PackObjectType.TREE => GitObjectIO.CreateTreePayload(targetObject),
            PackObjectType.BLOB => GitObjectIO.CreateBlobPayload(targetObject),
            _ => throw new ArgumentOutOfRangeException(nameof(baseType))
        };

    private byte[] ReadBaseObject(string baseObject, int size)
    {
        return GitObjectIO.ReadObject(repo, baseObject, stream =>
        {
            using var ins = new InflaterInputStream(stream);
            using var br = new BinaryReader(ins);
            while (br.ReadByte() != '\0') ;
        
            var uncompressedBaseObject = new byte[size];
            _ = br.Read(uncompressedBaseObject, 0, size);

            return uncompressedBaseObject;
        });
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
