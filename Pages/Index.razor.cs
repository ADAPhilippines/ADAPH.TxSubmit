using Microsoft.AspNetCore.Components;
using ADAPH.TxSubmit.Data;
using MudBlazor;
using ADAPH.TxSubmit.Services;
using System.ComponentModel;
using ADAPH.TxSubmit.Models;
using System.Web;
using Blockfrost.Api.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ADAPH.TxSubmit.Pages;

public partial class Index : IDisposable
{
	[Inject] private TxSubmitDbContext? _dbContext { get; set; }
	[Inject] private ILogger<Index>? _logger { get; set; }
	[Inject] private TimeZoneService? TimeZoneService { get; set; }
	[Inject] private GlobalStateService? GlobalStateService { get; set; }
	[Inject] private IHttpClientFactory? HttpClientFactory { get; set; }
	[Inject] private IAddressesService? BlockfrostAddressService { get; set; }
	[Inject] private ISnackbar? Snackbar { get; set; }
	public ChartOptions ChartOptions = new ChartOptions();
	public List<ChartSeries> Series = new List<ChartSeries>();
	public string[] XAxisLabels = { };
	public bool IsJsInteropReady = false;
	private List<SubmittedTransaction> SubmittedTxs { get; set; } = new();
	private string ChartHeight { get; set; } = "450px";
	private string TabBarClass { get; set; } = "full-width-tabs-toolbar";
	private string WalletAddress { get; set; } = string.Empty;
	private bool IsLoading { get; set; }
	private string WalletAddressErrorMessage { get; set; } = "Wallet Address must not be empty.";
	private bool IsWalletAddressInvalid { get; set; }

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

			if (Snackbar is not null)
			{
				Snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomCenter;
				Snackbar.Configuration.MaxDisplayedSnackbars = 1;
				Snackbar.Configuration.NewestOnTop = true;
				Snackbar.Configuration.ShowTransitionDuration = 300;
				Snackbar.Configuration.HideTransitionDuration = 300;
				Snackbar.Configuration.VisibleStateDuration = 2000;
			}
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
			Name = "Txs Submitted",
			Data = hourlyPendingTxs.Select(d => (double)d.Item2).ToArray()
		});

		Series.Add(new()
		{
			Name = "Average Confirmation Time (minutes)",
			Data = hourlyAverageConfirmationTimes.Select(d => d.Item2.TotalMinutes).ToArray()
		});

		ChartOptions.YAxisTicks = 5;
		ChartOptions.ChartPalette = new string[] { "#594ae2ff", "#00c853ff" };
		await InvokeAsync(StateHasChanged);
	}

	private async Task<List<(DateTime, int)>> GetHourlyTxesAsync()
	{
		var hourlyPendingTxes = new List<(DateTime, int)>();

		if (TimeZoneService is null || GlobalStateService is null) return hourlyPendingTxes;

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

		if (GlobalStateService is null) return hourlyData;

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
			TabBarClass = string.Empty;
	}

	private async void OnConnectButtonClicked(MouseEventArgs args)
	{
		IsWalletAddressInvalid = false;
		WalletAddress = string.Empty;
		// Get wallets installed
		// Display wallet selection
		// Save selected wallet
		// Get stake address
		// Get Txs
		await InvokeAsync(StateHasChanged);
	}

	private async void OnRetrieveTransactionsButtonClicked(MouseEventArgs args)
	{
		try
		{
			IsWalletAddressInvalid = false;
			if (!string.IsNullOrEmpty(WalletAddress))
			{
				SubmittedTxs.Clear();
				IsLoading = true;
				await InvokeAsync(StateHasChanged);
				// Fetch Txs
				var stakeAddress = await GetStakeAddressAsync(WalletAddress);
				await FetchSubmittedTxsAsync(stakeAddress);
			}
			else
			{
				WalletAddressErrorMessage = "Wallet Address must not be empty.";
				IsWalletAddressInvalid = true;
			}
		}
		catch (Exception ex)
		{
			if (_logger is not null)
				_logger.Log(LogLevel.Error, ex.Message);
		}

		IsLoading = false;
		await InvokeAsync(StateHasChanged);
	}

	private async Task FetchSubmittedTxsAsync(string stakeAddress)
	{
		try
		{
			if (_dbContext is null) throw new Exception("DbContext is null.");
			if (HttpClientFactory is null) throw new Exception("HttpClientFactory is null.");

			var transactionOwners = _dbContext.Set<TransactionOwner>().AsNoTracking().Where(txOwner => txOwner.OwnerAddress == stakeAddress)
					.Include(to => to.Transaction)
					.OrderByDescending(txOwner => txOwner.DateCreated)
					.Take(20).ToArray();

			using var client = HttpClientFactory.CreateClient("tx-inspector");
			foreach (var transactionOwner in transactionOwners)
			{
				var transaction = transactionOwner.Transaction;
				if (transaction is not null && transaction.TxBytes is not null)
				{
					var txCbor64 = Convert.ToBase64String(transaction.TxBytes);
					var rawTx = await client.GetFromJsonAsync<RawTransaction>($"?txCbor64={HttpUtility.UrlEncode(txCbor64)}");

					if (rawTx is null) throw new Exception("Error occured while fetching transaction inspector response.");

					var status = GetTxStatus(transaction.DateCreated, transaction.DateConfirmed);
					SubmittedTxs.Add(
						new SubmittedTransaction
						{
							DateCreated = transactionOwner.DateCreated,
							RawTransaction = rawTx,
							Status = status
						}
					);
					await InvokeAsync(StateHasChanged);
				}
			}
		}
		catch (Exception ex)
		{
			if (_logger is not null)
				_logger.Log(LogLevel.Error, ex.Message);
			Snackbar?.Add("An error has occured. Unable to retrieve transactions.", Severity.Error);
		}
	}


	private TransactionStatus GetTxStatus(DateTime dateCreated, DateTime? dateConfirmed)
	{
		var currentDate = DateTime.UtcNow;
		if (dateConfirmed is not null)
		{
			if (currentDate - dateConfirmed < TimeSpan.FromMinutes(5))
				return TransactionStatus.Low;
			if (currentDate - dateConfirmed >= TimeSpan.FromMinutes(5))
				return TransactionStatus.Confirmed;
		}
		if (dateConfirmed is null)
		{
			if (currentDate - dateCreated < TimeSpan.FromDays(1))
				return TransactionStatus.Pending;
			if (currentDate - dateCreated >= TimeSpan.FromDays(1))
				return TransactionStatus.Rejected;
		}
		return TransactionStatus.Pending;
	}

	private async Task<string> GetStakeAddressAsync(string walletAddress)
	{
		try
		{
			if (BlockfrostAddressService is null) throw new Exception("Blockfrost address service is null.");
			var address = await BlockfrostAddressService.GetAddressesAsync(walletAddress);
			return address.StakeAddress;
		}
		catch (Exception ex)
		{
			if (_logger is not null)
				_logger.Log(LogLevel.Error, ex.Message);
			WalletAddressErrorMessage = "Wallet address is invalid.";
			IsWalletAddressInvalid = true;
			return string.Empty;
		}
	}

	public void Dispose()
	{
		if (GlobalStateService is not null)
			GlobalStateService.PropertyChanged -= GlobalStateServicePropertyChanged;
	}
}