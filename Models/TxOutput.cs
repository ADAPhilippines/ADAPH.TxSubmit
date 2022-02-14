
using System.Text.Json.Serialization;

namespace ADAPH.TxSubmit.Models;
public class TxOutput
{
	[JsonPropertyName("amount")]
	public Amount[] Amount { get; set; } = new Amount[] { };

	[JsonPropertyName("address")]
	public string Address { get; set; } = string.Empty;

	[JsonPropertyName("data_hash")]
	public string DataHash { get; set; } = string.Empty;

}