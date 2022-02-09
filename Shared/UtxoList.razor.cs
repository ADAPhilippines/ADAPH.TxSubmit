using ADAPH.TxSubmit.Models;
using Microsoft.AspNetCore.Components;

namespace ADAPH.TxSubmit.Shared;

public partial class UtxoList
{
	[Parameter] public Utxo[]? Utxos { get; set; }
}