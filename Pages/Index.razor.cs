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
	public ChartOptions chartOptions = new ChartOptions();
	public List<ChartSeries> Series = new List<ChartSeries>();
	public string[] XAxisLabels = { };
	public bool IsJsInteropReady = false;
	private List<SubmittedTx> SubmittedTxs { get; set; } = new();
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
			if (GlobalStateService is not null)
				await UpdateValuesAsync();

			InitializeTestTxs();
		}
		await base.OnAfterRenderAsync(firstRender);
	}

	private async Task UpdateValuesAsync()
	{
		if (!IsJsInteropReady) return;

		var hourlyPendingTxes = await GetHourlyTxesAsync();
		var hourlyAverageConfirmationTimes = GetHourlyAverageConfirmationTimes();

		XAxisLabels = hourlyPendingTxes
		  .Select(d => d.Item1.ToString("h tt"))
		  .ToArray();

		Series.Clear();

		Series.Add(new()
		{
			Name = "Txs Submitted",
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

	private void InitializeTestTxs()
	{
		var testInputs = new Utxo[] {
				new Utxo {
					Address = "addr1vx30za24ljla5mt6cjrpe80hsj9czgynczad9tkk5jxs47sta7g2n",
					Amount = "1,128.623873",
				},
			};

		var testOutputs = new Utxo[] {
				new Utxo {
					Address = "addr1vx30za24ljla5mt6cjrpe80hsj9czgynczad9tkk5jxs47sta7g2n",
					Amount = "167.967772",
					Assets = new Asset[]{
						new Asset {
							Quantity = "1",
							Name = "Token 1"
						},
						new Asset {
							Quantity = "1",
							Name = "Token 2"
						},
					}
				},
			};

		var testInputs2 = new Utxo[] {
				new Utxo {
					Address = "addr1vx30za24ljla5mt6cjrpe80hsj9czgynczad9tkk5jxs47sta7g2n",
					Amount = "1,128.623873"
				},
			};

		var testOutputs2 = new Utxo[] {
				new Utxo {
					Address = "addr1vx30za24ljla5mt6cjrpe80hsj9czgynczad9tkk5jxs47sta7g2n",
					Amount = "167.967772"
				},
				new Utxo {
					Address = "addr1vx30za24ljla5mt6cjrpe80hsj9czgynczad9tkk5jxs47sta7g2n",
					Amount = "167.967772"
				},
			};

		var submittedTx1 = new SubmittedTx
		{
			DateCreated = DateTime.UtcNow,
			TxId = "7a509627decef8b7f81a0ca459e379cb727f23b5e5ef41272f8f676f472dbefc",
			Amount = "129.93",
			Fee = "0.656101",
			Status = "Sent",
			Inputs = testInputs,
			Outputs = testOutputs
		};

		var submittedTx2 = new SubmittedTx
		{
			DateCreated = DateTime.UtcNow,
			TxId = "7a509627decef8b7f81a0ca459e379cb727f23b5e5ef41272f8f676f472dbefc",
			Amount = "129.93",
			Fee = "0.656101",
			Status = "Sent",
			Inputs = testInputs2,
			Outputs = testOutputs2
		};

		SubmittedTxs.Add(submittedTx1);
		SubmittedTxs.Add(submittedTx2);
	}

	public void Dispose()
	{
		if (GlobalStateService is not null)
			GlobalStateService.PropertyChanged -= GlobalStateServicePropertyChanged;
	}
}