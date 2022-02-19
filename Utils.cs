using System.Text;
using ADAPH.TxSubmit.Data;
using ADAPH.TxSubmit.Models;
using Microsoft.EntityFrameworkCore;

public static class Utils
{
  public static string LovelaceToAda(string lovelace)
  {
    var ada = string.Empty;
    return string.Format("{0:0.000000}", float.Parse(lovelace) / 1_000_000);
  }

  public static Amount[] GetAdaAssets(Amount[] amounts)
  {
    var adaAmountsList = new List<Amount>();
    var adaAmounts = amounts.Where(amount => amount.Unit == "lovelace");
    foreach (var adaAmount in adaAmounts)
    {
      adaAmountsList.Add
      (
        new Amount
        {
          Quantity = LovelaceToAda(adaAmount.Quantity),
          Unit = "₳"
        }
      );
    }
    return adaAmountsList.ToArray();
  }

  public static Amount[] GetOtherAssets(Amount[] amounts)
  {
    return amounts.Where(amount => amount.Unit != "lovelace").ToArray();
  }

  public static string HexToAscii(string hex)
  {
    var bytes = Enumerable.Range(0, hex.Length)
           .Where(x => x % 2 == 0)
           .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
           .ToArray();

    return Encoding.UTF8.GetString(bytes);
  }

  public static string FormatUnitText(string unit)
  {
    var policyId = unit.Substring(0, 56);
    var asciiUnit = HexToAscii(unit.Substring(56));
    return $"{policyId}.{asciiUnit}";
  }

  public static DbContextOptionsBuilder BuilderDbContextOptions(DbContextOptionsBuilder options, IConfiguration config)
  {
    var txSubmitPostgresConnStr = config.GetConnectionString("TxSubmitDbPostgres");
    if (!string.IsNullOrEmpty(txSubmitPostgresConnStr))
    {
      options.UseNpgsql(txSubmitPostgresConnStr);
    }
    else
    {
      var txSubmitSqliteConnStr = config.GetConnectionString("TxSubmitDbSqlite");
      if (!string.IsNullOrEmpty(txSubmitSqliteConnStr))
      {
        options.UseSqlite(txSubmitSqliteConnStr);
      }
      else
      {
        options.UseInMemoryDatabase("TxSubmitDb");
      }
    }
    return options;
  }
}