using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using Geocoding;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Reactive.Concurrency;

namespace GeoCoding.ViewModels
{
	public class MainWindowViewModel : ReactiveObject
	{
		public MainWindowViewModel(IAsyncGeocoder geoCoder)
		{
			this.GeoCoder = geoCoder;

			_startQueryCommand = new ReactiveCommand(
				this.WhenAnyValue(x => x.Address).Select(x => !string.IsNullOrWhiteSpace(x))
			);
			_startQueryCommand.RegisterAsyncTask<IList<string>>((_) => this.StartQuery(this._address))
				.ToProperty(this, x => x.ResultAddresses, out _resultAddresses);

			var errorMessage = "The geo coding information could not be obtained.";
			var errorResolution = "Check your internet connection.";

			_startQueryCommand.ThrownExceptions
				.Select(ex => new UserError(errorMessage, errorResolution, innerException: ex))
				.SelectMany(UserError.Throw)
				.Where(x => x == RecoveryOptionResult.RetryOperation)
				.InvokeCommand(_startQueryCommand);

			var addressChanges = this.WhenAnyValue(x => x.Address)
				.Throttle(TimeSpan.FromSeconds(1))
				.Where(x => this.StartQueryCommand.CanExecute(null))
				.InvokeCommand(_startQueryCommand);
		}

		private IAsyncGeocoder GeoCoder { get; set; }

		private string _address;
		public string Address
		{
			get { return _address; }
			set { this.RaiseAndSetIfChanged(ref _address, value); }
		}

		private ReactiveCommand _startQueryCommand;
		public ICommand StartQueryCommand
		{
			get { return _startQueryCommand; }
		}

		private ObservableAsPropertyHelper<IList<string>> _resultAddresses;
		public IList<string> ResultAddresses
		{
			get { return _resultAddresses.Value; }
		}

		private async Task<IList<string>> StartQuery(string address)
		{
			try
			{
				//System.Threading.Thread.Sleep(1000);

				var addresses = await this.GeoCoder.GeocodeAsync(address);
				return addresses.Select(a => String.Format("{0} ({1})", a.Coordinates.ToString(), a.FormattedAddress)).ToList();
			}
			catch
			{
				throw;
			}
		}
	}
}
