using System.ComponentModel;
using System.Runtime.CompilerServices;
using ADAPH.TxSubmit.Data;
namespace ADAPH.TxSubmit.Services;

public class GlobalStateService : INotifyPropertyChanged
{
	private int _totalPendingTxesCount { get; set; }
	public int TotalPendingTxesCount
	{
		get => _totalPendingTxesCount;
		set
		{
			_totalPendingTxesCount = value;
			NotifyPropertyChanged();
		}
	}

	private int _totalConfirmedTxesCount;
	public int TotalConfirmedTxesCount
	{
		get => _totalConfirmedTxesCount;
		set
		{
			_totalConfirmedTxesCount = value;
			NotifyPropertyChanged();
		}
	}

	private TimeSpan _averageConfirmationTime { get; set; }
	public TimeSpan AverageConfirmationTime
	{
		get => _averageConfirmationTime;
		set
		{
			_averageConfirmationTime = value;
			NotifyPropertyChanged();
		}
	}

	private List<Transaction> _hourlyCreatedTxes { get; set; } = new();
	public List<Transaction> HourlyCreatedTxes
	{
		get => _hourlyCreatedTxes;
		set
		{
			_hourlyCreatedTxes = value;
			NotifyPropertyChanged();
		}
	}

	private List<Transaction> _hourlyConfirmedTxes { get; set; } = new();
	public List<Transaction> HourlyConfirmedTxes
	{
		get => _hourlyConfirmedTxes;
		set
		{
			_hourlyConfirmedTxes = value;
			NotifyPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}