using System.Net.Http.Headers;
using System.Text.Json;
using ADAPH.TxSubmit.Data;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using Blockfrost.Api.Services;

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

      using var client = _httpClientFactory.CreateClient("tx-inspector");
      var base64String = Convert.ToBase64String(txBytes);
      var result = await client.GetFromJsonAsync<JsonElement>($"?txCbor64={HttpUtility.UrlEncode(base64String)}");

      var isHypeSkullOwned = false;
      var hypePolicyIds = new string[] {
        "2f459a0a0872e299982d69e97f2affdb22919cafe1732de01ca4b36c",
        "6f37a98bd0c9ced4e302ec2fb3a2f19ffba1b5c0c2bedee3dac30e56"
      };
      if (result.TryGetProperty("inputs", out var inputs))
      {
        var stakeAddresses = inputs.EnumerateArray()
          .Select<JsonElement, string?>(el => el.GetProperty("stakeAddress").GetString())
          .Where(s => s is not null)
          .Distinct();

        foreach(var stakeAddress in stakeAddresses)
        {
          var page = 1;
          var assetCount = 100;
          do
          {
            var assets = await _bfAccountsService.GetAddressesAssetsAsync(stakeAddress, 100, page++);
            assetCount = assets.Count;
            isHypeSkullOwned = assets.Any(a => hypePolicyIds.Any(p => a.Unit.Contains(p)));
          }
          while(assetCount == 100 && !isHypeSkullOwned);

          if(isHypeSkullOwned) break;
        }
      }

      if(isHypeSkullOwned)
      {
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