using ADAPH.TxSubmit.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ADAPH.TxSubmit.Shared;

public partial class TxExpansionPanel 
{
	[Parameter] public SubmittedTx? SubmittedTx { get; set; }
	private bool IsOpen { get; set; }
	private MudBlazor.Color AmountColor { get; set; } = MudBlazor.Color.Success;
	private Align AmountTextAlign { get; set; } = Align.Right;
	private string GetTotalOutputTokens() 
	{
		if(SubmittedTx is null || SubmittedTx.Outputs is null) return string.Empty;
		
		var totalTokens = 0;
		foreach(var utxo in SubmittedTx.Outputs) 
		{
			if(utxo.Assets is not null)
				totalTokens += utxo.Assets.Count();
		}
		if(totalTokens > 0)
		{
			return $"{totalTokens} Token(s)";
		}
		return string.Empty;
	}

	private void OnBreakPointChanged(Breakpoint breakpoint)
	{
		if(breakpoint >= Breakpoint.Md) 
			AmountTextAlign = Align.Right;
		else 
			AmountTextAlign = Align.Left;
	}
}