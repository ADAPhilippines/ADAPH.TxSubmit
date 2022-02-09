namespace ADAPH.TxSubmit.Models;

public class SubmittedTx
{
	public DateTime DateCreated { get; set; }
	public string TxId { get; set; } = string.Empty;
	public string Status { get; set; } = "Sent";
	public string Amount { get; set; } = string.Empty;
	public string Fee { get; set; } = string.Empty;
	public Utxo[]? Inputs { get; set; } = null;
	public Utxo[]? Outputs { get; set; } = null;
}