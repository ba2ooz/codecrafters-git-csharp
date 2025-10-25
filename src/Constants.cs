namespace codecrafters_git;

public class Constants
{
    // special bytes constants
    public const byte SpaceByte = (byte)' ';
    public const byte NullByte = 0;
    
    // pck line constants
    public const string FlushPkt = "0000";
    public const string DonePktLine = "done\n";
    public const int PckLineHeaderSize = 4;
    public const int HexBase = 16;
    
    // pack header constants
    public static readonly byte[] PackSignature = "PACK"u8.ToArray();
    public const int PackHeaderSize = 12;
    
    // pack object's header constants
    public const byte MSBMask = 0x80;
    public const byte ObjectSizeFirstByteMask = 0x0F;
    public const byte ObjectSizeNthByteMask = 0x7F;
    public const byte ObjectTypeByteMask = 0x07;
    public const int ObjectSizeFirstByteShift = 4;
    public const int ObjectSizeNthByteShift = 7;
    
    // pack object
    public const int HashLength = 20;
}