using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using ADAPH.TxSubmit.Data;
using MudBlazor;

namespace ADAPH.TxSubmit.Pages;

public partial class Index
{
  [Inject] private TxSubmitDbContext? _dbContext { get; set; }
  [Inject] private ILogger<Index>? _logger { get; set; }
  [Inject] private TimeZoneService? TimeZoneService { get; set; }
  public int TotalPendingTxesCount { get; set; }
  public int TotalConfirmedTxesCount { get; set; }
  public TimeSpan AverageConfirmationTime { get; set; }
  public ChartOptions chartOptions = new ChartOptions();

  public List<ChartSeries> Series = new List<ChartSeries>();
  public string[] XAxisLabels = { };
  private async Task<int> GetTotalPendingTxesCountAsync()
  {
    if (_dbContext is null) return 0;

    return await _dbContext.Transactions.Where(tx => tx.DateConfirmed == null).CountAsync();
  }


  protected override async Task OnInitializedAsync()
  {
    _ = InitializeDataRefresh();
    await base.OnInitializedAsync();
  }

  private async Task InitializeDataRefresh()
  {
    while (true)
    {
      if (_logger is not null)
      {
        _logger.LogInformation("Refreshing data...");
        await UpdateValuesAsync();
        await Task.Delay(TimeSpan.FromSeconds(20));
      }
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    await base.OnAfterRenderAsync(firstRender);
  }

  private async Task UpdateValuesAsync()
  {
    TotalPendingTxesCount = await GetTotalPendingTxesCountAsync();
    TotalConfirmedTxesCount = await GetTotalConfirmedTxesCountAsync();
    AverageConfirmationTime = await GetAverageConfirmationTimeAsync();
    var hourlyPendingTxes = await GetHourlyTxesAsync();
    var hourlyAverageConfirmationTimes = await GetHourlyAverageConfirmationTimesAsync();

    XAxisLabels = hourlyPendingTxes
      .Select(d => d.Item1.ToString("h tt"))
      .ToArray();

    Series.Clear();

    Series.Add(new()
    {
      Name = "Txes Submitted",
      Data = hourlyPendingTxes.Select(d => (double)d.Item2).ToArray()
    });

    Series.Add(new()
    {
      Name = "Average Confirmation Time (minutes)",
      Data = hourlyAverageConfirmationTimes.Select(d => d.Item2.TotalMinutes).ToArray()
    });

    chartOptions.YAxisTicks = 5;
    chartOptions.ChartPalette = new string[] { "#594ae2ff", "#00c853ff"};
    await InvokeAsync(StateHasChanged);
  }

  private async Task<int> GetTotalConfirmedTxesCountAsync()
  {
    if (_dbContext is null) return 0;

    return await _dbContext.Transactions
      .Where(tx => tx.DateConfirmed != null && DateTime.UtcNow - tx.DateConfirmed < TimeSpan.FromHours(1))
      .CountAsync();
  }

  private async Task<TimeSpan> GetAverageConfirmationTimeAsync()
  {
    if (_dbContext is null) return TimeSpan.FromSeconds(0);

    var timeSpans = await _dbContext.Transactions
        .Where(tx => tx.DateConfirmed != null && DateTime.UtcNow - tx.DateConfirmed < TimeSpan.FromHours(1))
        .Select(tx => tx.DateConfirmed - tx.DateCreated ?? TimeSpan.FromSeconds(0))
        .ToListAsync();

    if (timeSpans.Any())
      return TimeSpan.FromSeconds(timeSpans.Select(s => s.TotalSeconds).Average());
    else
      return TimeSpan.FromSeconds(0);
  }

  private async Task<List<(DateTime, int)>> GetHourlyTxesAsync()
  {
    var hourlyPendingTxes = new List<(DateTime, int)>();

    if (_dbContext is null ||
      TimeZoneService is null) return hourlyPendingTxes;

    var currentDate = DateTime.UtcNow;
    var txes = await _dbContext.Transactions
        .Where(tx => currentDate - tx.DateCreated < TimeSpan.FromHours(12))
        .ToListAsync();

    var binDate = currentDate.Subtract(TimeSpan.FromHours(12));

    while (binDate <= currentDate)
    {
      var count = txes
        .Where(tx => tx.DateCreated < binDate && tx.DateCreated >= binDate.Subtract(TimeSpan.FromHours(1)))
        .Count();
      

      var offset = await TimeZoneService.GetLocalDateTime(binDate);
      hourlyPendingTxes.Add((offset.DateTime, count));

      binDate += TimeSpan.FromHours(1);
    }

    return hourlyPendingTxes;
  }

  private async Task<List<(DateTime, TimeSpan)>> GetHourlyAverageConfirmationTimesAsync()
  {
    var hourlyData = new List<(DateTime, TimeSpan)>();

    if (_dbContext is null) return hourlyData;

    var currentDate = DateTime.UtcNow;
    var txes = await _dbContext.Transactions
        .Where(tx => tx.DateConfirmed != null && currentDate - tx.DateConfirmed < TimeSpan.FromHours(12))
        .ToListAsync();

    var binDate = currentDate.Subtract(TimeSpan.FromHours(12));

    while (binDate <= currentDate)
    {
      var durations = txes
          .Where(tx => tx.DateConfirmed < binDate && tx.DateConfirmed >= binDate.Subtract(TimeSpan.FromHours(1)))
          .Select(tx => (tx.DateConfirmed - tx.DateCreated)?.TotalSeconds ?? 0)
          .ToList();

      if (durations.Any())
      {
        var average = TimeSpan.FromSeconds(durations.Average());
        hourlyData.Add((binDate, average));
      }
      else
        hourlyData.Add((binDate, TimeSpan.FromSeconds(0)));

      binDate += TimeSpan.FromHours(1);
    }


    return hourlyData;
  }

  private string FormatTimeSpan(TimeSpan ts)
  {
    return string.Format("{0:%h}H {0:%m}M {0:%s}S", ts);
  }
}