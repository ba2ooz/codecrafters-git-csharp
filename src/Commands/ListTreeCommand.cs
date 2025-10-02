using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace codecrafters_git.Commands;

public class ListTreeCommand : ICommand
{
    public void Run(string[] args)
    {
        // tree <size>\0
        //     <mode> <name>\0<20_byte_sha>
        //     <mode> <name>\0<20_byte_sha>
        
        var flag = args[1];
        var hash = args[2];
        
        var gitObjectHash = args[2];
        var gitObjectDir = gitObjectHash[..2];
        var gitObjectName = gitObjectHash[2..];
        var gitObjectPath = $".git/objects/{gitObjectDir}/{gitObjectName}";
        var outputBuilder = new StringBuilder();
        var sortedOutput = new SortedDictionary<string, string>();
        
        try
        {
            using var fs = File.OpenRead(gitObjectPath);
            using var zlib = new ZLibStream(fs, CompressionMode.Decompress);
            using var memStream = new MemoryStream();
            zlib.CopyTo(memStream);
            memStream.Position = 0;
            using var reader = new BinaryReader(memStream);
            while (reader.ReadByte() != 0); // skip the header part
            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                byte b;
                
                // read mode
                var modeBytes = new List<byte> ();
                while ((b = reader.ReadByte()) != (byte)' ')
                    modeBytes.Add(b);
                var modeChars = Encoding.ASCII.GetChars(modeBytes.ToArray());
                var modeStr = new string(modeChars);

                // read object name
                var objectNameBytes = new List<byte> ();
                while ((b = reader.ReadByte()) != 0)
                    objectNameBytes.Add(b);
                var objectNameChars = Encoding.ASCII.GetChars(objectNameBytes.ToArray());
                var objectNameStr = new string(objectNameChars);
                
                // read object hash
                var objectHashBuffer = reader.ReadBytes(20);
                var objectHashStr = BitConverter.ToString(objectHashBuffer)
                    .Replace("-", "")
                    .ToLowerInvariant();

                sortedOutput.Add(objectNameStr, $"{GetObjectType(modeStr)} {objectHashStr}\t{objectNameStr}{Environment.NewLine}");
            }

            foreach (var pair in sortedOutput)
            {
                if (args[1] == "--name-only")
                    Console.WriteLine(pair.Key);
                else
                    Console.Write(pair.Value);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    

    private string GetObjectType(string mode) => mode switch
    {

        "40000" => "040000 tree",
        "100644" or "100755" or "120000" => $"{mode} blob",
        _ => "wrong parsing"
    };
}