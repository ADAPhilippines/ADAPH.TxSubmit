namespace ADAPH.TxSubmit.Data;

public record TransactionOwner
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public DateTime DateCreated { get; set; } = DateTime.UtcNow;
  public string StakingKey { get; set; } = string.Empty;

  public Guid TransactionId { get; set; }
  public Transaction? Transaction { get; set; } 
}