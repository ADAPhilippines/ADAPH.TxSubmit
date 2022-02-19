using ADAPH.TxSubmit.Data;
using ADAPH.TxSubmit.Services;
using Microsoft.EntityFrameworkCore;

namespace ADAPH.TxSubmit.Workers;

public class DbWorker : BackgroundService
{
  private readonly IConfiguration _configuration;
  private GlobalStateService? _globalStateService { get; set; }
  private readonly ILogger<DbWorker> _logger;

  public DbWorker(
    IConfiguration configuration,
    GlobalStateService globalStateService,
    ILogger<DbWorker> logger)
  {
    _configuration = configuration;
    _globalStateService = globalStateService;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (true)
    {
      if (_logger is not null)
      {
        try
        {
          _logger.LogInformation("Refreshing data...");
          await UpdateValuesAsync();
        }
        catch (Exception ex)
        {
          _logger.LogError(ex.Message);
        }
      }
      else
      {
        throw new NullReferenceException("Logger is null.");
      }
      await Task.Delay(TimeSpan.FromSeconds(60));
    }
  }

  private async Task UpdateValuesAsync()
  {
    if (_globalStateService is not null)
    {
      var optionsBuilder = new DbContextOptionsBuilder<TxSubmitDbContext>();
      using var _dbContext = new TxSubmitDbContext(Utils.BuilderDbContextOptions(optionsBuilder, _configuration).Options);
      _globalStateService.TotalPendingTxesCount = await GetTotalPendingTxesCountAsync(_dbContext);
      _globalStateService.TotalConfirmedTxesCount = await GetTotalConfirmedTxesCountAsync(_dbContext);
      _globalStateService.AverageConfirmationTime = await GetAverageConfirmationTimeAsync(_dbContext);
      _globalStateService.HourlyCreatedTxes = await GetHourlyCreatedTxes(_dbContext);
      _globalStateService.HourlyConfirmedTxes = await GetHourlyConfirmedTxes(_dbContext);
    }
  }

  private async Task<int> GetTotalPendingTxesCountAsync(TxSubmitDbContext dbContext)
  {
    if (dbContext is null) return 0;

    return await dbContext.Transactions
      .Where(tx => tx.DateConfirmed == null)
			.Where(tx => tx.DateCreated >= DateTime.UtcNow.AddHours(-1))
      .CountAsync();
  }

  private async Task<int> GetTotalConfirmedTxesCountAsync(TxSubmitDbContext dbContext)
  {
    if (dbContext is null) return 0;

    var currentDate = DateTime.UtcNow;

    return await dbContext.Transactions
      .Where(tx => tx.DateConfirmed != null)
			.Where(tx => tx.DateConfirmed >= currentDate.AddHours(-1))
      .CountAsync();
  }

  private async Task<TimeSpan> GetAverageConfirmationTimeAsync(TxSubmitDbContext dbContext)
  {
    if (dbContext is null) return TimeSpan.FromSeconds(0);

    var currentDate = DateTime.UtcNow;

    var timeSpans = await dbContext.Transactions
      .Where(tx => tx.DateConfirmed != null)
			.Where(tx => tx.DateConfirmed >= currentDate.AddHours(-1))
      .Select(tx => tx.DateConfirmed - tx.DateCreated ?? TimeSpan.FromSeconds(0))
      .ToListAsync();

    if (timeSpans.Any())
      return TimeSpan.FromSeconds(timeSpans.Select(s => s.TotalSeconds).Average());
    else
      return TimeSpan.FromSeconds(0);
  }

  private async Task<List<Transaction>> GetHourlyCreatedTxes(TxSubmitDbContext dbContext)
  {
    if (dbContext is null) return new();

    return await dbContext.Transactions
      .Where(tx => tx.DateCreated >= DateTime.UtcNow.AddHours(-13))
      .ToListAsync();
  }

  private async Task<List<Transaction>> GetHourlyConfirmedTxes(TxSubmitDbContext dbContext)
  {
    if (dbContext is null) return new();
    return await dbContext.Transactions
      .Where(tx => tx.DateConfirmed != null)
			.Where(tx => tx.DateConfirmed >= DateTime.UtcNow.AddHours(-13))
      .ToListAsync();
  }
}