using Microsoft.AspNetCore.Components;
using ADAPH.TxSubmit.Data;
using MudBlazor;
using ADAPH.TxSubmit.Services;
using System.ComponentModel;
using ADAPH.TxSubmit.Models;

namespace ADAPH.TxSubmit.Pages;

public partial class Index : IDisposable
{
	[Inject] private TxSubmitDbContext? _dbContext { get; set; }
	[Inject] private ILogger<Index>? _logger { get; set; }
	[Inject] private TimeZoneService? TimeZoneService { get; set; }
	[Inject] private GlobalStateService? GlobalStateService { get; set; }
	[Inject] private HttpClient? Http { get; set; }
	public ChartOptions chartOptions = new ChartOptions();
	public List<ChartSeries> Series = new List<ChartSeries>();
	public string[] XAxisLabels = { };
	public bool IsJsInteropReady = false;
	private List<SubmittedTransaction> SubmittedTxs { get; set; } = new();
	private string ChartHeight { get; set; } = "450px";
	private string TabBarClass { get; set; } = "full-width-tabs-toolbar";

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
			IsJsInteropReady = true;
			await InitializeTestTxsAsync();
			if (GlobalStateService is not null)
				await UpdateValuesAsync();

		}
		await base.OnAfterRenderAsync(firstRender);
	}

	private async Task UpdateValuesAsync()
	{
		if (!IsJsInteropReady) return;

		var hourlyPendingTxs = await GetHourlyTxesAsync();
		var hourlyAverageConfirmationTimes = GetHourlyAverageConfirmationTimes();

		XAxisLabels = hourlyPendingTxs
		  .Select(d => d.Item1.ToString("h tt"))
		  .ToArray();

		Series.Clear();

		Series.Add(new()
		{
			Name = "Txes Submitted",
			Data = hourlyPendingTxs.Select(d => (double)d.Item2).ToArray()
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

		var txes = GlobalStateService.HourlyCreatedTxes;

		var currentDate = DateTime.UtcNow;
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

		var txes = GlobalStateService.HourlyConfirmedTxes;

		var currentDate = DateTime.UtcNow;
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

	private void OnBreakPointChanged(Breakpoint breakpoint)
	{
		if (breakpoint > Breakpoint.Sm)
			ChartHeight = "450px";
		else
			ChartHeight = "100%";

		if (breakpoint >= Breakpoint.Md)
			TabBarClass = "full-width-tabs-toolbar";
		else
			TabBarClass = "";
	}

	private async Task InitializeTestTxsAsync()
	{
		try 
		{
			if (Http is not null)
			{	
				var txCbor64 = "hKYAgoJYIKwdWm8vPjLj+NlBqUUEgICKXcWk6zYRIMSjO5vL2crGAYJYID6LP0xHsM28IXua9cQ1UkK8NAupVd8aqDdZnNo76mzgAQGCglg5AbRL9LNobdmbC+0OUYaBOOfwgZUvQEn50k6vd8MnPDKRu7BV7EfIcm+G0BVja+aziR2qok3I82YUghoAHoSAoVgc+OHymaZ0Um6BsnxXyNHSzhSgS3FXH14DrOgYsqFLSURLYXRhbmFib2kBglg5AbRL9LNobdmbC+0OUYaBOOfwgZUvQEn50k6vd8MnPDKRu7BV7EfIcm+G0BVja+aziR2qok3I82YUGgBARh0CGgAHoSAHWCB7i/9EZJZje/oJdqSd3dSg53Q2yKZec1XU0qIvkaREYQmhWBz44fKZpnRSboGyfFfI0dLOFKBLcVcfXgOs6BiyoUtJREthdGFuYWJvaQEOgVgctEv0s2ht2ZsL7Q5RhoE45/CBlS9ASfnSTq93w6EBgYIAWBy0S/SzaG3ZmwvtDlGGgTjn8IGVL0BJ+dJOr3fD9aEZHMiheDhmOGUxZjI5OWE2NzQ1MjZlODFiMjdjNTdjOGQxZDJjZTE0YTA0YjcxNTcxZjVlMDNhY2U4MThiMqFrSURLYXRhbmFib2mhZmF2YXRhcqJocHJvdG9jb2xkaXBmc2NzcmN4LlFtTmhoaEpUWVBmU3Z3eXNhSDk4YnQ1NWdrNWk1YmhGRVVBQW1ackZFUXFYdjE=";
				
				var rawTx = await Http.GetFromJsonAsync<RawTransaction>($"https://tx.adaph.io/?txCbor64={txCbor64}");
				if(rawTx is null) return;
				SubmittedTxs.Add(
					new SubmittedTransaction
					{
						DateCreated = DateTime.UtcNow,
						RawTransaction = rawTx
					}
				);
			}
		}
		catch(Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
	}

	public void Dispose()
	{
		if (GlobalStateService is not null)
			GlobalStateService.PropertyChanged -= GlobalStateServicePropertyChanged;
	}
}