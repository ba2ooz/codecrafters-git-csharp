namespace codecrafters_git.Commands.Delta;

public class DeltaParser
{
    const byte DeltaFirstByteMask = 0x7F; 
    const int DeltaFirstByteShift = 7; 
    const byte MSBMask = 0x80;
    
    public static Records.Delta Parse(byte[] delta)
    {
        var pos = 0;
        var deltaObject = new Records.Delta()
        {
            SourceLength = readVariableIntegerValue(delta, ref pos),
            TargetLength = readVariableIntegerValue(delta, ref pos),
            Instructions = ParseDeltaInstructions(delta, ref pos),
        };
        
        // var sourceVarLength = readVariableIntegerValue(delta, ref pos);
        // var targetVarLength = readVariableIntegerValue(delta, ref pos);

        // Console.WriteLine("source length: " + sourceVarLength);
        // Console.WriteLine("target length: " + targetVarLength);

        // var deltaInstructions = ParseDeltaInstructions(delta, ref pos);

        return deltaObject;
    }
   

    public static int readVariableIntegerValue(byte[] delta, ref int position)
    {
        var firstHeaderByte = delta[position];
        var size = firstHeaderByte & 0x7F;          // 0x7F => 0111 1111 => do AND 0x7F to get the value of bits 0-6
        var shift = 7;
                
        var nextHeaderByte = firstHeaderByte; 
        //Console.WriteLine("header byte: " + Convert.ToHexString([nextHeaderByte]) + "byte pos: " + position);
    
        while ((nextHeaderByte & 0x80) != 0)           // 0x80 => 1000 0000 => AND 0x80 to get the value of MSB
        {
            position++;
            nextHeaderByte = delta[position];
            var currByteSizePart = (nextHeaderByte & 0x7F) << shift; // 0x7F => 0111 1111 => do AND to get the rest of 7 bits, THEN shift left by shift positions to be able to concatenate the existing size in the correct order
            size |= currByteSizePart;                                   // OR to concatenate existing size
            shift += 7;
            //Console.WriteLine("header byte: " + Convert.ToHexString([nextHeaderByte]) + "byte pos: " + position);
        } 
        position++;
    
        return size;
    }
    
    static DeltaInstructionList ParseDeltaInstructions(byte[] delta, ref int position)
    {
        var instructions = new DeltaInstructionList();
        while (position < delta.Length)
        {
            var currByte = delta[position];
            if ((currByte & MSBMask) != 0)
                ParseCopyInstructions(instructions, delta, ref position);
            else
                ParseInsertInstructions(instructions, delta, ref position);
        }
        
        return instructions;
    }
    
    static int[] GetPresentOffsetBytes(byte inputByte)
    {
        var offset1 = inputByte & 0x01;
        var offset2 = inputByte & 0x02;
        var offset3 = inputByte & 0x04;
        var offset4 = inputByte & 0x08;
        return [ offset1, offset2, offset3, offset4 ];
    }
    
    static int[] GetPresentSizeBytes(byte inputByte)
    {
        var size1 = inputByte & 0x10;
        var size2 = inputByte & 0x20;
        var size3 = inputByte & 0x40;
        return [ size1, size2, size3];
    }
    
    static int CalculateVariableBytesValue(int[] nextBytesFlags, byte[] delta, ref int position)
    {
        var dynamicValue = 0;
        for (var i = 0; i < nextBytesFlags.Length; i++)
        {
            // if 0 next byte is either 0 or not part of the current variable integer value
            if (nextBytesFlags[i] == 0)
                continue;
            
            var nextByte = delta[++position];
            var shiftedByte = nextByte << (i * 8);
            dynamicValue |= shiftedByte;
        }
        
        return dynamicValue;
    }
    
    static void ParseCopyInstructions(DeltaInstructionList instructions, byte[] delta, ref int position)
    {
        var currByte = delta[position];
        var offsets = GetPresentOffsetBytes(currByte);
        var sizes = GetPresentSizeBytes(currByte);
        var copyOffset = CalculateVariableBytesValue(offsets, delta, ref position);
        var copySize = CalculateVariableBytesValue(sizes, delta, ref position);
        instructions.AddCopy(copyOffset,  copySize);
    
        position++;
        
        //Console.WriteLine("Instruction Copy " + copySize + " bytes, starting at offset " + copyOffset);
    }
    
    static void ParseInsertInstructions(DeltaInstructionList instructions, byte[] delta, ref int position)
    {
        var insertSize = (int)delta[position++];
        instructions.AddInsert(delta[position..(position + insertSize)]);
        //Console.WriteLine("Insert " + insertSize + " from the delta");
        position+= insertSize;
    }
}