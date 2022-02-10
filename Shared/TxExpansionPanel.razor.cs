using ADAPH.TxSubmit.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ADAPH.TxSubmit.Shared;

public partial class TxExpansionPanel
{
	[Parameter] public SubmittedTransaction? SubmittedTransaction { get; set; }
	private bool IsOpen { get; set; }
	private MudBlazor.Color AmountColor { get; set; } = MudBlazor.Color.Secondary;
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

		float amount = 0;
		foreach (var output in SubmittedTransaction.RawTransaction.Outputs)
		{
			var adaAmounts = Utils.GetAdaAssets(output.Amount);
			foreach (var ada in adaAmounts)
			{
				amount += float.Parse(ada.Quantity);
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