using ADAPH.TxSubmit.Data;
using Blockfrost.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ADAPH.TxSubmit.Workers;

public class TransactionWorker : BackgroundService
{

  private readonly ITransactionsService _txService;
  private readonly ILogger<TransactionWorker> _logger;
  private readonly IConfiguration _configuration;

  public TransactionWorker(
    IConfiguration configuration,
    ITransactionsService txService,
    ILogger<TransactionWorker> logger)
  {
    _txService = txService;
    _logger = logger;
    _configuration = configuration;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      var optionsBuilder = new DbContextOptionsBuilder<TxSubmitDbContext>();
      optionsBuilder.UseNpgsql(_configuration.GetConnectionString("TxSubmitDb"));
      var _dbContext = new TxSubmitDbContext(optionsBuilder.Options);

      var unconfirmedTx = await _dbContext.Transactions
       .Where(tx => tx.DateConfirmed == null)
       .ToListAsync();

      if (unconfirmedTx is not null)
      {
        _logger.Log(LogLevel.Information, $"Checking for unconfirmed transactions: {unconfirmedTx.Count}");
        foreach (var tx in unconfirmedTx)
        {
          try
          {
            var getTxResponse = await _txService.GetAsync(tx.TxHash, stoppingToken);
            if (getTxResponse is not null)
            {
              tx.DateConfirmed = DateTime.UtcNow;
              await _dbContext.SaveChangesAsync();
            }
          }
          catch
          {
            _logger.Log(LogLevel.Information, $"Tx not yet confirmed: [{tx.TxHash}]");
          }
        }
      }

      await Task.Delay(1000 * 60, stoppingToken);
    }
  }
}