namespace codecrafters_git.Delta;

public static class DeltaParser
{
    public static Records.Delta Parse(byte[] delta)
    {
        var pos = 0;
        return new Records.Delta()
        {
            SourceLength = ReadVariableIntegerValue(delta, ref pos),
            TargetLength = ReadVariableIntegerValue(delta, ref pos),
            Instructions = ParseDeltaInstructions(delta, ref pos),
        };
    }
    
    private static DeltaInstructionList ParseDeltaInstructions(byte[] delta, ref int position)
    {
        var instructions = new DeltaInstructionList();
        while (position < delta.Length)
        {
            var currByte = delta[position];
            if ((currByte & Constants.MSBMask) != 0)
                ParseCopyInstructions(instructions, delta, ref position);
            else
                ParseInsertInstructions(instructions, delta, ref position);
        }
        
        return instructions;
    }
    
    private static void ParseCopyInstructions(DeltaInstructionList instructions, byte[] delta, ref int position)
    {
        var currByte = delta[position];
        var offsets = GetPresentOffsetBytes(currByte);
        var sizes = GetPresentSizeBytes(currByte);
        var copyOffset = CalculateVariableBytesValue(offsets, delta, ref position);
        var copySize = CalculateVariableBytesValue(sizes, delta, ref position);
        instructions.AddCopy(copyOffset,  copySize);
    
        position++;
    }
    
    private static void ParseInsertInstructions(DeltaInstructionList instructions, byte[] delta, ref int position)
    {
        var insertSize = (int)delta[position++];
        instructions.AddInsert(delta[position..(position + insertSize)]);
        position+= insertSize;
    }
    
    private static int[] GetPresentOffsetBytes(byte inputByte)
    {
        var offset1 = inputByte & 0x01;
        var offset2 = inputByte & 0x02;
        var offset3 = inputByte & 0x04;
        var offset4 = inputByte & 0x08;
        return [ offset1, offset2, offset3, offset4 ];
    }
    
    private static int[] GetPresentSizeBytes(byte inputByte)
    {
        var size1 = inputByte & 0x10;
        var size2 = inputByte & 0x20;
        var size3 = inputByte & 0x40;
        return [ size1, size2, size3];
    }
    
    private static int CalculateVariableBytesValue(int[] nextBytesFlags, byte[] delta, ref int position)
    {
        var dynamicValue = 0;
        for (var i = 0; i < nextBytesFlags.Length; i++)
        {
            // if 0, the next byte is either 0 or not part of the current variable integer value
            if (nextBytesFlags[i] == 0)
                continue;
            
            var nextByte = delta[++position];
            var shiftedByte = nextByte << (i * 8);
            dynamicValue |= shiftedByte;
        }
        
        return dynamicValue;
    }

    private static int ReadVariableIntegerValue(byte[] delta, ref int position)
    {
        var firstHeaderByte = delta[position];
        var size = firstHeaderByte & Constants.ObjectSizeNthByteMask; // 0x7F => 0111 1111 => do AND 0x7F to get the value of bits 0-6
        var shift = Constants.ObjectSizeNthByteShift;
                
        var nextHeaderByte = firstHeaderByte; 
    
        while ((nextHeaderByte & Constants.MSBMask) != 0) // 0x80 => 1000 0000 => AND 0x80 to get the value of MSB
        {
            position++;
            nextHeaderByte = delta[position];
            var currByteSizePart = (nextHeaderByte & Constants.ObjectSizeNthByteMask) << shift; // 0x7F => 0111 1111 => do AND to get the rest of 7 bits, THEN shift left by shift positions to be able to concatenate the existing size in the correct order
            size |= currByteSizePart;                                                              // OR to concatenate the existing size
            shift += Constants.ObjectSizeNthByteShift;
        } 
        position++;
    
        return size;
    }
}