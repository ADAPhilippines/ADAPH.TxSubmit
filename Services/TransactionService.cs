using System.Net.Http.Headers;
using System.Text.Json;

public sealed class TransactionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public TransactionService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string?> SubmitAsync(byte[] txBytes)
    {
        using var client = _httpClientFactory.CreateClient();
        var byteContent = new ByteArrayContent(txBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/cbor");
        var txResponse = await client.PostAsync(_configuration["CardanoTxSubmitEndpoint"], byteContent);

        if (!txResponse.IsSuccessStatusCode) return null;
        
        var txIdResponse = await txResponse.Content.ReadFromJsonAsync<JsonElement>();
        return txIdResponse.GetString();
    }
}