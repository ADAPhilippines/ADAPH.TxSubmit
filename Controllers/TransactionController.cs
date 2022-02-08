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
  private readonly TransactionService _transactionService;

  public TransactionController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    TxSubmitDbContext dbContext,
    TransactionService transactionService)
  {
    _httpClientFactory = httpClientFactory;
    _configuration = configuration;
    _dbContext = dbContext;
    _transactionService = transactionService;
  }

  [HttpPost]
  public async Task<IActionResult> SubmiTx()
  {
    using var client = _httpClientFactory.CreateClient();
    using var ms = new MemoryStream();
    await Request.Body.CopyToAsync(ms);
    var txBytes = ms.ToArray();
    var txId = await _transactionService.SubmitAsync(txBytes);
  
    if (txId != null)
    {
      var tx = new Transaction
      {
        TxHash = txId,
        TxBytes = txBytes,
        TxSize = txBytes.Length
      };
      _dbContext.Transactions.Add(tx);
      await _dbContext.SaveChangesAsync();

      HttpContext.Response.StatusCode = 202;
      return new JsonResult(txId);
    }
    else
    {
      return BadRequest();
    }
  }
}