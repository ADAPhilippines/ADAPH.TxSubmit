namespace ADAPH.TxSubmit.Models;

public partial class Utxo
{
	public string Address { get; set; } = string.Empty;
	public string Amount { get; set; } = string.Empty;
	public Asset[]? Assets { get; set; } = null;
}