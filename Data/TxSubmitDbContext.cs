using Microsoft.EntityFrameworkCore;

namespace ADAPH.TxSubmit.Data;

public class TxSubmitDbContext : DbContext
{
  public virtual DbSet<Transaction> Transactions { get; set; }

  public TxSubmitDbContext(DbContextOptions<TxSubmitDbContext> options)
    : base(options)
  {
  }
}