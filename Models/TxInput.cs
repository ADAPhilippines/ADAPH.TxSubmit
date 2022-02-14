using System.Text.Json.Serialization;

namespace ADAPH.TxSubmit.Models;
public class TxInput
{
	[JsonPropertyName("txId")]
	public string TxId { get; set; } = string.Empty;

	[JsonPropertyName("txIdx")]
	public int TxIdx { get; set; } = -1;

	[JsonPropertyName("address")]
	public string Address { get; set; } = string.Empty;

	[JsonPropertyName("stakeAddress")]
	public string StakeAddress { get; set; } = string.Empty;

	[JsonPropertyName("amount")]
	public Amount[] Amount { get; set; } = new Amount[] { };
}