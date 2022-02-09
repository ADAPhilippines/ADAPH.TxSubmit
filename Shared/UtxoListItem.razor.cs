using ADAPH.TxSubmit.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ADAPH.TxSubmit.Shared;

public partial class UtxoListItem
{
	[Parameter] public Utxo? Utxo { get; set; }
	private Align TokenTextAlign { get; set; } = Align.Right;

	private void OnBreakPointChanged(Breakpoint breakpoint)
	{
		if (breakpoint >= Breakpoint.Md)
			TokenTextAlign = Align.Right;
		else
			TokenTextAlign = Align.Left;
	}
}