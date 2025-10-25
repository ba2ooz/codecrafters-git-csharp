using System.Net.Http.Headers;

namespace codecrafters_git.Protocol;

public class GitHttpClient(HttpClient client)
{
    private const string GetRefsUri = "/info/refs?service=git-upload-pack";
    private const string GetPackUri = "/git-upload-pack";
    
    public async Task<string> GetRefAdvertismentAsync(string repoUrl)
    {
        var response = await client.GetAsync($"{repoUrl}{GetRefsUri}");
        return await response.Content.ReadAsStringAsync();
    }
    
    public async Task<byte[]> GetPackAsync(string repoUrl, byte[] negotiationPayload)
    {
        var content = new ByteArrayContent(negotiationPayload);
        var res = await client.PostAsync($"{repoUrl}{GetPackUri}", content);
        return await res.Content.ReadAsByteArrayAsync();
    }
}