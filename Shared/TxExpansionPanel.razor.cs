using ADAPH.TxSubmit.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ADAPH.TxSubmit.Shared;

public partial class TxExpansionPanel
{
	[Parameter] public SubmittedTransaction? SubmittedTransaction { get; set; }
	[Inject] private TimeZoneService? TimeZoneService { get; set; }
	private bool IsOpen { get; set; }
	private MudBlazor.Color AmountColor { get; set; } = MudBlazor.Color.Success;
	private MudBlazor.Color StatusChipColor { get; set; } = MudBlazor.Color.Success;
	private string SideBarColorClass { get; set; } = string.Empty;
	private Align AmountTextAlign { get; set; } = Align.Right;
	private DateTimeOffset LocalDateTime { get; set; }

	protected override async Task OnParametersSetAsync()
	{
		if(TimeZoneService is not null && SubmittedTransaction is not null)
			LocalDateTime = await TimeZoneService.GetLocalDateTime(SubmittedTransaction.DateCreated);
		SetStatusColor();
		await InvokeAsync(StateHasChanged);
	}

	private void SetStatusColor()
	{
		if(SubmittedTransaction is not null)
		{
			switch(SubmittedTransaction.Status)
			{
				case TransactionStatus.Confirmed:
					StatusChipColor = MudBlazor.Color.Success;
					SideBarColorClass = "tx-expansion-sidebar tx-sidebar-confirmed";
					break;
				case TransactionStatus.Rejected:
					StatusChipColor = MudBlazor.Color.Error;
					SideBarColorClass = "tx-expansion-sidebar tx-sidebar-rejected";
					break;
				case TransactionStatus.Low:
					StatusChipColor = MudBlazor.Color.Info;
					SideBarColorClass = "tx-expansion-sidebar tx-sidebar-low";
					break;
				case TransactionStatus.Pending:
					StatusChipColor = MudBlazor.Color.Warning;
					SideBarColorClass = "tx-expansion-sidebar tx-sidebar-pending";
					break;
				default:
					break;
			}
		}
	}
	
	private string GetTotalOutputTokens()
	{
		if (SubmittedTransaction is null ||
				SubmittedTransaction.RawTransaction is null ||
				SubmittedTransaction.RawTransaction.Outputs is null) return string.Empty;

		var totalTokens = 0;
		foreach (var utxo in SubmittedTransaction.RawTransaction.Outputs)
		{
			if (utxo.Amount is not null)
				totalTokens += utxo.Amount.Where(amount => amount.Unit != "lovelace").Count();
		}
		if (totalTokens > 0)
		{
			return $"{totalTokens} Token(s)";
		}
		return string.Empty;
	}

	private string GetOutputAmount()
	{
		if (SubmittedTransaction is null ||
				SubmittedTransaction.RawTransaction is null ||
				SubmittedTransaction.RawTransaction.Outputs is null) return string.Empty;

		double amount = 0;
		foreach (var output in SubmittedTransaction.RawTransaction.Outputs)
		{
			var adaAmounts = Utils.GetAdaAssets(output.Amount);
			foreach (var ada in adaAmounts)
			{
				amount += double.Parse(ada.Quantity);
			}
		}
		return string.Format("{0:0.000000} ₳", amount);
	}

	private void OnBreakPointChanged(Breakpoint breakpoint)
	{
		if (breakpoint >= Breakpoint.Md)
			AmountTextAlign = Align.Right;
		else
			AmountTextAlign = Align.Left;
	}
}