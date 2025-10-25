using System.Text;

namespace codecrafters_git.Protocol;

public static class GitProtocolParser
{
    public static List<string> ParseRefs(string refAdvertisment)
    {
        var refs = new List<string>();
        var pos = 0;

        while (pos < refAdvertisment.Length)
        {
            var firstLineLengthBytePosition = pos;
            var lastLineLengthBytePosition = firstLineLengthBytePosition + Constants.PckLineHeaderSize;
            var packLineLengthHex = refAdvertisment[firstLineLengthBytePosition..lastLineLengthBytePosition];
            var pktLineLength = Convert.ToInt32(packLineLengthHex, Constants.HexBase);
       
            if (pktLineLength is 0 or Constants.PckLineHeaderSize)
            {
                if ((pos + Constants.PckLineHeaderSize) >= refAdvertisment.Length)
                    break;
                    
                pos += Constants.PckLineHeaderSize;
                continue;
            }
        
            var lineContent = refAdvertisment[lastLineLengthBytePosition..(lastLineLengthBytePosition + pktLineLength)];
            refs.Add(lineContent.Split("\n")[0]);
            pos += pktLineLength;
        }

        return refs;
    }
    
    public static byte[] CreateNegotiationPayload(string commitHash)
    {
        var wantLine = $"want {commitHash} ofs-delta\n";
        var wantPktLine = CreatePktLine(wantLine);
        var donePktLine = CreatePktLine(Constants.DonePktLine);
        
        var payload = $"{wantPktLine}{Constants.FlushPkt}{donePktLine}";
        return Encoding.ASCII.GetBytes(payload);
    }
    
    private static string CreatePktLine(string content)
    {
        var length = content.Length + Constants.PckLineHeaderSize;
        return $"{length:x4}{content}";
    }
}
