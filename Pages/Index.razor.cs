using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using ADAPH.TxSubmit.Data;
using MudBlazor;
using ADAPH.TxSubmit.Services;
using System.ComponentModel;

namespace ADAPH.TxSubmit.Pages;

public partial class Index : IDisposable
{
	[Inject] private TxSubmitDbContext? _dbContext { get; set; }
	[Inject] private ILogger<Index>? _logger { get; set; }
	[Inject] private TimeZoneService? TimeZoneService { get; set; }
	[Inject] private GlobalStateService? GlobalStateService { get; set; }
	public ChartOptions chartOptions = new ChartOptions();
	public List<ChartSeries> Series = new List<ChartSeries>();
	public string[] XAxisLabels = { };

	protected override async Task OnInitializedAsync()
	{
		if (GlobalStateService is not null)
			GlobalStateService.PropertyChanged += GlobalStateServicePropertyChanged;
		await base.OnInitializedAsync();
	}

	private async void GlobalStateServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		await UpdateValuesAsync();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			if (GlobalStateService is not null)
				await UpdateValuesAsync();
		}
		await base.OnAfterRenderAsync(firstRender);
	}

	private async Task UpdateValuesAsync()
	{
		var hourlyPendingTxes = await GetHourlyTxesAsync();
		var hourlyAverageConfirmationTimes = GetHourlyAverageConfirmationTimes();

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
		chartOptions.ChartPalette = new string[] { "#594ae2ff", "#00c853ff" };
		await InvokeAsync(StateHasChanged);
	}
	private async Task<List<(DateTime, int)>> GetHourlyTxesAsync()
	{
		var hourlyPendingTxes = new List<(DateTime, int)>();

		if (_dbContext is null ||
		  TimeZoneService is null || GlobalStateService is null) return hourlyPendingTxes;

		var currentDate = DateTime.UtcNow;
		var txes = GlobalStateService.HourlyCreatedTxes;

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

	private List<(DateTime, TimeSpan)> GetHourlyAverageConfirmationTimes()
	{
		var hourlyData = new List<(DateTime, TimeSpan)>();

		if (_dbContext is null || GlobalStateService is null) return hourlyData;

		var currentDate = DateTime.UtcNow;
		var txes = GlobalStateService.HourlyConfirmedTxes;
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

	public void Dispose()
	{
		if (GlobalStateService is not null)
			GlobalStateService.PropertyChanged -= GlobalStateServicePropertyChanged;
	}
}