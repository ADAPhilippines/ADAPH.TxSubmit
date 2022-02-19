using Microsoft.EntityFrameworkCore;

namespace ADAPH.TxSubmit.Data;

public class TxSubmitDbContext : DbContext
{
  public virtual DbSet<Transaction> Transactions { get; set; }
  public virtual DbSet<TransactionOwner> TransactionOwners { get; set; }

  public TxSubmitDbContext(DbContextOptions options)
    : base(options)
  {
  }
}