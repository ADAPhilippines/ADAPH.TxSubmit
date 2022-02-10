using ADAPH.TxSubmit.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ADAPH.TxSubmit.Shared;

public partial class TxExpansionPanel
{
	[Parameter] public SubmittedTransaction? SubmittedTransaction { get; set; }
	private bool IsOpen { get; set; }
	private MudBlazor.Color AmountColor { get; set; } = MudBlazor.Color.Success;
	private Align AmountTextAlign { get; set; } = Align.Right;

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

		var amount = string.Empty;
		foreach (var utxo in SubmittedTransaction.RawTransaction.Outputs)
		{
			if (utxo.Amount is not null)
				amount = utxo.Amount.Where(amount => amount.Unit == "lovelace").First().Quantity;
		}
		return amount;
	}

	private void OnBreakPointChanged(Breakpoint breakpoint)
	{
		if (breakpoint >= Breakpoint.Md)
			AmountTextAlign = Align.Right;
		else
			AmountTextAlign = Align.Left;
	}
}