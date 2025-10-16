using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using codecrafters_git.Commands.Delta;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace codecrafters_git.Commands;

public class CloneCommand : ICommand
{
    private string ReadBaseObjectName(byte[] pack, ref int pos)
    {
        byte[] name = new byte[20];
        int objNameEnd = pos + 20;
        int counter = 0;
        while (pos < objNameEnd)
            name[counter++] = pack[pos++];

        //Console.WriteLine("obj name positions read " + counter);
        //Console.WriteLine("obj name length " + name.Length);
        //Console.WriteLine("obj name data " + Convert.ToHexStringLower(name));

        //Console.WriteLine(Convert.ToHexString([pack[pos], pack[pos+1]]));
        return Convert.ToHexStringLower(name);
    }
    
    private int SkipPackHeader(byte[] pack)
    {
        var iter = 0;
        while (iter < pack.Length)
        {
            if (pack[iter] == 0x50 &&       // P
                pack[iter + 1] == 0x41 &&   // A
                pack[iter + 2] == 0x43 &&   // C
                pack[iter + 3] == 0x4B)     // K
                return iter + 12; // skip "PACK", <pack version> and <nr_of_objects> each using 4 bytes
            
            iter++;
        }
        
        Console.WriteLine(Encoding.ASCII.GetString(pack[..4]));
        Console.WriteLine(Encoding.ASCII.GetString(pack[4..8]));
        Console.WriteLine(Encoding.ASCII.GetString(pack[8..12]));
        Console.WriteLine(BitConverter.ToInt32(pack[12..16].Reverse().ToArray(), 0));
        Console.WriteLine(BitConverter.ToInt32(pack[16..20].Reverse().ToArray(), 0));
        //Console.WriteLine("objects in pack hex:" + BitConverter.ToInt32(pack[(iter-4)..(iter-1)].Reverse().ToArray(), 0));
        // read until 'PACK version object_count' and return the next position -> should be implemented
        return iter;
    }
    
    private byte[] GetPack(HttpClient client, string url, byte[] negotiationPayload)
    {
        // var request = new StringBuilder();
        // request.Append("0032want 355b1e074b8d4e3bcd6f906f6136c9965a491461\n");
        // request.Append("00000009done\n");
        var body = negotiationPayload; //Encoding.ASCII.GetBytes(request.ToString());

        // Console.WriteLine("post request :");
        // Console.WriteLine(request.ToString());
        
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-git-upload-pack-request");
        
        client.DefaultRequestHeaders.Add("Accept", "application/x-git-upload-pack-result"); 
        var res = client.PostAsync($"{url}/git-upload-pack", content).Result;
        var pack = res.Content.ReadAsByteArrayAsync().Result;
        
        // Console.WriteLine("rs after post");
        // Console.WriteLine(res.Headers.ToString());
        // Console.WriteLine(Convert.ToHexString(pack));
        //Console.WriteLine("got pack");
        return pack;
    }
    
    private string GetRefAdvertisment(HttpClient client, string url)
    {
        var response = client.GetAsync($"{url}/info/refs?service=git-upload-pack").Result;
        var result = response.Content.ReadAsStringAsync().Result;
        //Console.WriteLine(result[..1000]);
        return result;
    }

    private List<string> ParseRefAdvertisment(string refAdvertisments)
    {
        var refs = new List<string>();
        var iter = 0;

        while (iter < refAdvertisments.Length)
        {
            var firstLineLengthBytePosition = iter;
            var lastLineLengthBytePosition = firstLineLengthBytePosition + 4;
            var packLineLengthHex = refAdvertisments[firstLineLengthBytePosition..lastLineLengthBytePosition];
            var pktLineLength = Convert.ToInt32(packLineLengthHex, 16);
       
            if (pktLineLength is 0 or 4)
            {
                if ((iter + 4) >= refAdvertisments.Length)
                    break;
                    
                iter += 4;
                continue;
            }
        
            var lineContent = refAdvertisments[lastLineLengthBytePosition..(lastLineLengthBytePosition + pktLineLength)];
            refs.Add(lineContent.Split("\n")[0]);
            iter += pktLineLength;
        }

        // Console.WriteLine("refs: ");
        // refs.ForEach(Console.WriteLine);

        return refs;
    }

    private byte[] ComposeNegotiationPayload(List<string> availableRefs)
    {
        var refHash = availableRefs[1].Split(" HEAD")[0];
        var wantRefLine = $"want {refHash} ofs-delta\n";
        var wantRefLineLength = wantRefLine.Length+4;
        var wantRefLineLengthHex = wantRefLineLength.ToString("x4");
        var endLineHex = 0.ToString("x4");
        
        var doneNegotiatingLine = "done\n";
        var doneNegotiatingLineLength = doneNegotiatingLine.Length + 4;
        var doneNegotiatingLineLengthHex = doneNegotiatingLineLength.ToString("x4");
        
        var wantRefLinePktLine = $"{wantRefLineLengthHex}{wantRefLine}{endLineHex}{doneNegotiatingLineLengthHex}{doneNegotiatingLine}";
        //Console.WriteLine("want line: " + wantRefLinePktLine);
        return Encoding.ASCII.GetBytes(wantRefLinePktLine);
    }
    
    public void Run(string[] args)
    {
        var git_url = $"{args[1]}";
        var local_repo_destination = args[2];

        var destination = local_repo_destination;
        if (destination == ".")
            destination = Directory.GetCurrentDirectory();
        else 
            Directory.CreateDirectory(destination);
        
        RepoInfo.RootDirectory = destination;

        new InitCommand().Run([]);
        
        var client = new HttpClient();
        var refsInPktLineFormat = GetRefAdvertisment(client, git_url);
        var refs = ParseRefAdvertisment(refsInPktLineFormat);
        var negotiation = ComposeNegotiationPayload(refs);
        var pack = GetPack(client, git_url, negotiation);
        // File.WriteAllBytes($"{Directory.GetCurrentDirectory()}/pack.pack", pack);
        // var pack = File.ReadAllBytes(git_url);
        var pos = SkipPackHeader(pack);
        var lastCommit = UnpackObjectEntries(pack, pos);
        
        var commitContent = CatFileCommand.DecompressFile(lastCommit);
        Console.WriteLine("last commit: " + commitContent);
        var commitTree = commitContent.Split("tree ")[1].Split("\n")[0];
        
        new ListTreeCommand().SaveTreeRecursively(destination, commitTree);
    }

    private string UnpackObjectEntries(byte[] pack, int pos)
    {
        var firstCommit = "";
        var indexObjectHashMap = new Dictionary<int, string>();
        var objectHashTypeMap = new Dictionary<string, PackObjectType>();
        
        while (pos < pack.Length - 20)
        {
            //Console.WriteLine("pos before reading header " + pos);
            var baseObject = "";
            var deltaNegativeOffset = 0;
            var headerPosition = pos;
            var (type, uncompressed_size) = GetObjectMetadata(pack, ref pos);
            
            Console.WriteLine("obj type: " + type);
            // Console.WriteLine("obj uncompressed_size: " + uncompressed_size);
            // Console.WriteLine("pos after reading header " + pos);
            
            if (type is PackObjectType.REF_DELTA)
            {
                baseObject = ReadBaseObjectName(pack, ref pos);
            } else if (type is PackObjectType.OFS_DELTA)
            {
                deltaNegativeOffset = ReadOfsDeltaOffset(pack, ref pos);
                
                Console.WriteLine("Got ofs delta");
            }
            
            // Create inflater - it tracks bytes consumed
            // var inflater = new Inflater();
            // using var ms = new MemoryStream(pack, pos, pack.Length - pos - 20);
            // using var ins = new InflaterInputStream(ms, inflater);
            //
            // byte[] uncompressedData = new byte[uncompressed_size];
            // _ = ins.Read(uncompressedData, 0, uncompressed_size);
            
            var inflater = new Inflater();
            inflater.SetInput(pack, pos, pack.Length - pos - 20);
            using var msOut = new MemoryStream();
            var buffer = new byte[4096];
            while (!inflater.IsFinished && !inflater.IsNeedingInput)
            {
                int count = inflater.Inflate(buffer);
                if (count == 0 && inflater.IsNeedingInput)
                    break;

                msOut.Write(buffer, 0, count);
            }

            byte[] uncompressedData = msOut.ToArray();

            
            if (type is PackObjectType.REF_DELTA)
            {
                var deltaResolver = new DeltaResolver();
                var baseType = objectHashTypeMap[baseObject];
                var resolvedDelta = deltaResolver.ResolveDelta(baseObject, baseType, uncompressedData);
                uncompressedData = resolvedDelta; 
            } else if (type is PackObjectType.OFS_DELTA)
            {
                var baseObjectPosition = headerPosition - deltaNegativeOffset;
                baseObject = indexObjectHashMap[baseObjectPosition];
                var baseType = objectHashTypeMap[baseObject];
                var resolvedDelta =  new DeltaResolver().ResolveDelta(baseObject, baseType, uncompressedData);
                uncompressedData = resolvedDelta;
            }
            
            var objSha = CompressAndSavePacketObject(type, uncompressedData);
            if (type is PackObjectType.COMMIT && string.IsNullOrEmpty(firstCommit))
                firstCommit = objSha;
            
            indexObjectHashMap.Add(headerPosition, objSha);
            if (type is PackObjectType.REF_DELTA or PackObjectType.OFS_DELTA)
                type = objectHashTypeMap[baseObject]; // to save as the base type
            
            objectHashTypeMap.Add(objSha, type);
            
            Console.WriteLine(headerPosition + " " + objSha);
            Console.WriteLine();
            
            Console.WriteLine("pos with totalIn: " + (pos + inflater. TotalIn));
            
            var nextPackObjHeaderPos = pos + inflater. TotalIn;//pos + inflater.TotalIn;
            pos = (int)nextPackObjHeaderPos;
            // Console.WriteLine("unprocessed bytes " + inflater.RemainingInput);
            // Console.WriteLine("compressed bytes read during inflate " + inflater.TotalIn);
            // Console.WriteLine("bytes read after inflate " + uncompressedBytesRead);
            // Console.WriteLine("next obj header pos " + nextPackObjHeaderPos);
            // Console.WriteLine("pack lenght " + pack.Length);
            // Console.WriteLine("uncompressed data: " + Encoding.ASCII.GetString(uncompressedData));    
        }

        return firstCommit;
    }

    private string CompressAndSavePacketObject(PackObjectType type, byte[] objectData) => type switch
    {
        PackObjectType.COMMIT => HashObjectCommand.CompressObject(WriteCommitCommand.GetRawCommit(Encoding.ASCII.GetString(objectData))),
        PackObjectType.TREE => HashObjectCommand.CompressObject(WriteTreeCommand.GetRawTreePayload(objectData)),
        PackObjectType.BLOB => HashObjectCommand.CompressObject(HashObjectCommand.GetRawBlob(Encoding.ASCII.GetString(objectData))),
        PackObjectType.TAG => "no need to proccess tag objects",
        PackObjectType.REF_DELTA => HashObjectCommand.CompressObject(objectData),
        PackObjectType.OFS_DELTA => HashObjectCommand.CompressObject(objectData),

        _ => $"{(int)type} unsupported"
    };

    private static PackObjectMeta GetObjectMetadata (byte[] pack, ref int position)
    {
        var firstHeaderByte = pack[position];
        var size = firstHeaderByte & 0x0F;          // 0x0F => 0000 1111 => do AND 0x0F to get the value of bits 0-3
        var type =  (firstHeaderByte >> 4) & 0x07;  // 0x07 => 0000 0111 => shift right 4 places to bring the bits 4-6 to position 0-2, THEN do AND 0x07 to get their value
        var shift = 4;
            
        var nextHeaderByte = firstHeaderByte; 
        //Console.WriteLine("header byte: " + Convert.ToHexString([nextHeaderByte]) + "byte pos: " + position);

        while ((nextHeaderByte & 0x80) != 0)           // 0x80 => 1000 0000 => AND 0x80 to get the value of MSB
        {
            position++;
            nextHeaderByte = pack[position];
            var currByteSizePart = (nextHeaderByte & 0x7F) << shift; // 0x7F => 0111 1111 => do AND to get the rest of 7 bits, THEN shift left by shift positions to be able to concatenate the existing size in the correct order
            size |= currByteSizePart;                                   // OR to concatenate existing size
            shift += 7;
            //Console.WriteLine("header byte: " + Convert.ToHexString([nextHeaderByte]) + "byte pos: " + position);
        } 
        position++;

        return new PackObjectMeta((PackObjectType)type, size);
    }
    
    private record PackObjectMeta(PackObjectType type, int size);
    
    public static int ReadOfsDeltaOffset(byte[] data, ref int position)
    {
        byte c = data[position++];
        int offset = c & 0x7F;

        while ((c & 0x80) != 0)
        {
            c = data[position++];
            offset = ((offset + 1) << 7) | (c & 0x7F);
        }

        return offset;
    }
}


public enum PackObjectType
{
    COMMIT = 1,
    TREE = 2,
    BLOB = 3,
    TAG = 4,
    OFS_DELTA = 6,
    REF_DELTA = 7
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