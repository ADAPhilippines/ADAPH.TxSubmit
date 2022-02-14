using System.Text.Json.Serialization;

namespace ADAPH.TxSubmit.Models;

public class Amount
{
	[JsonPropertyName("unit")]
	public string Unit { get; set; } = string.Empty;
	
	[JsonPropertyName("quantity")]
	public string Quantity { get; set; } = string.Empty;
}