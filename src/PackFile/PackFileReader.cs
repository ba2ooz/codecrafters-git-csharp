using codecrafters_git.Objects;
using codecrafters_git.Records;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace codecrafters_git.PackFile;

public class PackfileReader
{
    public int FindPackDataStart(byte[] pack)
    {
        for (var i = 0; i < pack.Length - Constants.PackSignature.Length; i++)
            if (pack[i] == Constants.PackSignature[0] &&      // P
                pack[i + 1] == Constants.PackSignature[1] &&  // A
                pack[i + 2] == Constants.PackSignature[2] &&  // C
                pack[i + 3] == Constants.PackSignature[3])    // K
                return i + Constants.PackHeaderSize;
        
        throw new InvalidDataException("Pack header not found");
    }

    /// <summary>
    /// Reads the header of a packed object, extracting its type and size.
    /// The type and size of each packed object are encoded in a variable-length format, where each byte contains
    /// part of the object size. The most significant bit (MSB) of each byte determines if
    /// the next byte is also part of the object size.
    /// Only the first byte contains both - type and size information. The remaining bytes contain the size information.
    /// First Byte Format Exmaple: [xtttssss] => [x] = MSB, [t] = type, [s] = size
    /// </summary>
    /// <param name="pack">The byte array containing the packed data.</param>
    /// <param name="position">
    /// A reference to the current position within the byte array. This value will
    /// be updated to the position after the object header has been read.
    /// </param>
    /// <returns>The header of the packed object, containing the object type and size.</returns>
    public PackObjectHeader ReadObjectHeader(byte[] pack, ref int position)
    {
        var firstByte = pack[position];
        var size = firstByte & Constants.ObjectSizeFirstByteMask;
        var type = (firstByte >> Constants.ObjectSizeFirstByteShift) 
                   & Constants.ObjectTypeByteMask;
        var shift = Constants.ObjectSizeFirstByteShift;
        
        var currentByte = firstByte;
        
        // Read variable-length size
        while ((currentByte & Constants.MSBMask) != 0)
        {
            position++;
            currentByte = pack[position];
            var sizePart = (currentByte & Constants.ObjectSizeNthByteMask) << shift;
            size |= sizePart;
            shift += Constants.ObjectSizeNthByteShift;
        }
        
        position++;
        
        return new PackObjectHeader((PackObjectType)type, size);
    }
    
    /// <summary>
    /// Read the hash of the object that the delta is based on.
    /// </summary>
    /// <param name="pack">The byte array containing the packed data.</param>
    /// <param name="position">
    /// A reference to the current position within the byte array. This value will be
    /// updated to the position after the delta offset has been read.
    /// </param>
    /// <returns>Base object hash of the current delta object</returns>
    public string ReadDeltaReferenceHash(byte[] pack, ref int position)
    {
        var hashBytes = new byte[Constants.HashLength];
        Array.Copy(pack, position, hashBytes, 0, Constants.HashLength);
        position += Constants.HashLength;
        return Convert.ToHexStringLower(hashBytes);
    }

    /// <summary>
    /// Reads the negative delta offset value from the specified byte array at the given position.
    /// The delta offset is encoded in a variable-length format, where each byte contains
    /// part of the offset. The most significant bit (MSB) of each byte determines if
    /// the next byte is also part of the offset.
    /// </summary>
    /// <param name="pack">The byte array containing the packed data.</param>
    /// <param name="position">
    /// A reference to the current position within the byte array. This value will be
    /// updated to the position after the delta offset has been read.
    /// </param>
    /// <returns>The computed delta offset value.</returns>
    public int ReadDeltaOffset(byte[] pack, ref int position)
    {
        var currentByte = pack[position++];
        var offset = currentByte & Constants.ObjectSizeNthByteMask;
        
        while ((currentByte & Constants.MSBMask) != 0)
        {
            currentByte = pack[position++];
            offset = ((offset + 1) << Constants.ObjectSizeNthByteShift) | (currentByte & Constants.ObjectSizeNthByteMask);
        }

        return offset;
    }
    
    public byte[] DecompressObject(byte[] pack, ref int position, int maxLength)
    {
        using var output = new MemoryStream();
        var buffer = new byte[4096];
        
        var inflater = new Inflater();
        inflater.SetInput(pack, position, maxLength);
        
        while (inflater is { IsFinished: false, IsNeedingInput: false })
        {
            var count = inflater.Inflate(buffer);
            if (count == 0 && inflater.IsNeedingInput)
                break;
            
            output.Write(buffer, 0, count);
        }
        
        position += (int) inflater.TotalIn;
        return output.ToArray();
    }
}