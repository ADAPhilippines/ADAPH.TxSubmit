using System.Net.Http.Headers;
using System.Text.Json;
using ADAPH.TxSubmit.Data;
using Microsoft.AspNetCore.Mvc;

namespace ADAPH.TxSubmit.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/tx/submit")]
[ApiVersion("1.0")]
public class TransactionController : ControllerBase
{
  private readonly IHttpClientFactory _httpClientFactory;
  private readonly IConfiguration _configuration;
  private readonly TxSubmitDbContext _dbContext;

  public TransactionController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    TxSubmitDbContext dbContext)
  {
    _httpClientFactory = httpClientFactory;
    _configuration = configuration;
    _dbContext = dbContext;
  }

  [HttpPost]
  public async Task<IActionResult> SubmiTx()
  {
    using var client = _httpClientFactory.CreateClient();
    using var ms = new MemoryStream();
    await Request.Body.CopyToAsync(ms);
    var byteContent = new ByteArrayContent(ms.ToArray());
    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/cbor");
    var txResponse = await client.PostAsync(_configuration["CardanoTxSubmitEndpoint"], byteContent);
    if (!txResponse.IsSuccessStatusCode) return BadRequest();
    var txIdResponse = await txResponse.Content.ReadFromJsonAsync<JsonElement>();
    var txId = txIdResponse.GetString();
    HttpContext.Response.StatusCode = 202;
  
    if (txId != null)
    {
      var tx = new Transaction
      {
        TxHash = txId
      };
      _dbContext.Transactions.Add(tx);
      await _dbContext.SaveChangesAsync();
      return new JsonResult(txId);
    }
    else
    {
      return BadRequest();
    }
  }
}