using ADAPH.TxSubmit.Data;
using ADAPH.TxSubmit.Services;
using Blockfrost.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ADAPH.TxSubmit.Workers;

public class TransactionWorker : BackgroundService
{

  private readonly ITransactionsService _bfTxService;
  private readonly TransactionService _txService;
  private readonly ILogger<TransactionWorker> _logger;
  private readonly IConfiguration _configuration;
  private readonly GlobalStateService _globalStateService;

  public TransactionWorker(
    IConfiguration configuration,
    ITransactionsService bfTxService,
    ILogger<TransactionWorker> logger,
    TransactionService txService,
    GlobalStateService globalStateService)
  {
    _bfTxService = bfTxService;
    _logger = logger;
    _configuration = configuration;
    _txService = txService;
    _globalStateService = globalStateService;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        var optionsBuilder = new DbContextOptionsBuilder<TxSubmitDbContext>();
        using var _dbContext = new TxSubmitDbContext(Utils.BuilderDbContextOptions(optionsBuilder, _configuration).Options);

        await CheckConfirmedTxAsync(_dbContext, stoppingToken);
        await CheckUnconfirmedTxsAsync(_dbContext, stoppingToken);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex.Message);
      }
      
      await Task.Delay(1000 * 60, stoppingToken);
    }
  }

  private async Task CheckConfirmedTxAsync(TxSubmitDbContext dbContext, CancellationToken stoppingToken)
  {
    //Get confirmed Txs with less than 10 confirmations
    var confirmedTxs = await dbContext.Transactions
       .Where(tx => tx.DateConfirmed != null)
       .Where(tx => tx.DateConfirmed >= DateTime.UtcNow.AddMinutes(-10))
       .ToListAsync();

    if (confirmedTxs is null) return;

    foreach (var tx in confirmedTxs)
    {
      //Check if tx is still confirmed
      var isTxConfirmed = await IsTxConfirmedAsync(tx, stoppingToken);

      if (!isTxConfirmed)
      {
        //Tx may have been affected by a fork
        //Mark tx as unconfirmed to be resubmitted again
        _logger.Log(LogLevel.Information, $"Transaction may have been affected by a fork: {tx.TxHash}");
        tx.DateConfirmed = null;
      }
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task CheckUnconfirmedTxsAsync(TxSubmitDbContext dbContext, CancellationToken stoppingToken)
  {
    var averageConfirmationTime = _globalStateService.AverageConfirmationTime;

    var unconfirmedTxs = await dbContext.Transactions
      .Where(tx => tx.DateConfirmed == null)
      .Where(tx => tx.DateCreated >= DateTime.UtcNow.AddHours(-12))
       .ToListAsync();

    if (unconfirmedTxs is null) return;

    _logger.Log(LogLevel.Information, $"Checking for unconfirmed transactions: {unconfirmedTxs.Count}");
    foreach (var tx in unconfirmedTxs)
    {
      var isTxConfirmed = await IsTxConfirmedAsync(tx, stoppingToken);
      if (isTxConfirmed)
      {
        //If tx is confirmed, updated db
        tx.DateConfirmed = DateTime.UtcNow;
      }
      else
      {
        if (tx.TxBytes is null ||
          DateTime.UtcNow - tx.DateCreated < averageConfirmationTime) continue;

        //if Tx is not confirmed,
        //Check if txBytes is stored in db and
        //Unconfirmed tx age is greater than average confirmation time
        //Then resubmit tx
        _logger.Log(LogLevel.Information, $"Resubmitting Tx: {tx.TxHash}");
        var txId = await _txService.SubmitAsync(tx.TxBytes);

        //If tx is resubmitted succesfully, slide DateCreated Value
        if (txId is not null && txId.Length == 64)
        {
          _logger.Log(LogLevel.Information, $"Transaction resubmitted: {tx.TxHash}");
          tx.DateCreated = DateTime.UtcNow;
        }
      }
    }
    await dbContext.SaveChangesAsync();
  }

  private async Task<bool> IsTxConfirmedAsync(Transaction tx, CancellationToken stoppingToken)
  {
    try
    {
      var getTxResponse = await _bfTxService.GetAsync(tx.TxHash, stoppingToken);
      return getTxResponse is not null;
    }
    catch
    {
      _logger.Log(LogLevel.Information, $"Tx not yet confirmed: [{tx.TxHash}]");
      return false;
    }
  }
}