
using System.Text.Json.Serialization;

namespace ADAPH.TxSubmit.Models;
public class RawTransaction
{
	[JsonPropertyName("hash")]
	public string Hash { get; set; } = string.Empty;

	[JsonPropertyName("inputs")]
	public TxInput[] Inputs { get; set; } = new TxInput[] { };

	[JsonPropertyName("outputs")]
	public TxOutput[] Outputs { get; set; } = new TxOutput[] { };

	[JsonPropertyName("fee")]
	public string Fee { get; set; } = string.Empty;

	[JsonPropertyName("size")]
	public ulong Size { get; set; }
}