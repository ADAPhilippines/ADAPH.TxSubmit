namespace ADAPH.TxSubmit.Models;

public class SubmittedTransaction
{
	public DateTime DateCreated { get; set; }
	public RawTransaction? RawTransaction { get; set; }
	public string Status { get; set; } = string.Empty;
}