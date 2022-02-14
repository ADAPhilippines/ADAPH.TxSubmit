namespace ADAPH.TxSubmit.Models;

public enum TransactionStatus
{
	Pending,
	Low,
	Confirmed,
	Rejected
}

public class SubmittedTransaction
{
	public DateTime DateCreated { get; set; }
	public RawTransaction? RawTransaction { get; set; }
	public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
}