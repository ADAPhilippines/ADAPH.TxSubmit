using System.Text.Json;
using System.Web;
using ADAPH.TxSubmit.Data;
using Blockfrost.Api.Services;
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
  private readonly IAccountsService _bfAccountsService;
  private readonly ILogger<TransactionController> _logger;

  public TransactionController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    TxSubmitDbContext dbContext,
    TransactionService transactionService,
    IAccountsService bfAccountsService,
    ILogger<TransactionController> logger)
  {
    _httpClientFactory = httpClientFactory;
    _configuration = configuration;
    _dbContext = dbContext;
    _transactionService = transactionService;
    _bfAccountsService = bfAccountsService;
    _logger = logger;
  }

  [HttpPost]
  public async Task<IActionResult> SubmiTx()
  {
    try
    {
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

        using var client = _httpClientFactory.CreateClient("tx-inspector");
        var base64String = Convert.ToBase64String(txBytes);
        var result = await client.GetFromJsonAsync<JsonElement>($"?txCbor64={HttpUtility.UrlEncode(base64String)}");

        if (result.TryGetProperty("inputs", out var inputs))
        {
          var stakeAddresses = inputs.EnumerateArray()
            .Select<JsonElement, string?>(el => ((el.GetProperty("stakeAddress").GetString()?.Length ?? 0) != 0) ? 
                el.GetProperty("stakeAddress").GetString() : el.GetProperty("address").GetString())
            .First(s => s is not null);

          if(stakeAddresses is not null)
          {
            _dbContext.TransactionOwners.Add(new () 
            {
              OwnerAddress = stakeAddresses,
              Transaction = tx
            });
          }
        }

        await _dbContext.SaveChangesAsync();

        HttpContext.Response.StatusCode = 202;
        return new JsonResult(txId);
      }
      else
      {
        return BadRequest();
      }
    }
    catch(Exception ex)
    {
      _logger.LogError(ex.Message);
      return BadRequest();
    }
  }
}