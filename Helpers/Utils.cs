using ADAPH.TxSubmit.Models;

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
		foreach(var adaAmount in adaAmounts)
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
}