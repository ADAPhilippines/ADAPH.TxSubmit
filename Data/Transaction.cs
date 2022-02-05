namespace ADAPH.TxSubmit.Data;

public record Transaction
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public string TxHash { get; set; } = string.Empty;
  public DateTime DateCreated { get; set; } = DateTime.UtcNow;
  public DateTime? DateConfirmed { get; set; } = null;
}