using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Currencies;
using Currencies.Data;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Diagnostics;
using System.Windows.Input;

namespace CurrenyTicker.ViewModels
{
	public class MainWindowViewModel : ReactiveObject
	{
		public MainWindowViewModel(ICurrencyRateProvider currenctyRateProvider)
		{

			this.Provider = currenctyRateProvider;

			InitializeCommands();

			InitializeActiveCurrencies();
			InitializeAllCurrencies();
		}

		private void InitializeActiveCurrencies()
		{
			this.ActiveCurrencies = new ReactiveList<string>();
			this.ActiveCurrencyStreamModels = new ReactiveList<CurrencyStream>();
			this.ActiveCurrencyStreams = this.ActiveCurrencyStreamModels.CreateDerivedCollection(m => new CurrencyViewModel(m));

			this.ActiveCurrencies.ItemsAdded
				.Subscribe(c => this.ActiveCurrencyStreamModels.Add(
					new CurrencyStream(c, this.Provider.RateUpdates.Where(r => r.TargetCurrency.Currency == c).Select(r => r.TargetCurrency.Value))
				));
			this.ActiveCurrencies.ItemsRemoved
				.Subscribe(c =>
					this.ActiveCurrencyStreamModels.Where(r => r.Currency == c).ToList().ForEach(x => this.ActiveCurrencyStreamModels.Remove(x))
				);

			this.AddActiveCurrency("GBP");
			this.AddActiveCurrency("USD");
			this.AddActiveCurrency("CHF");
			this.AddActiveCurrency("JPY");
		}

		private async void InitializeAllCurrencies()
		{
			var currencies = await this.Provider.GetAvailableCurrencies();
			var models = currencies
				.OrderBy(c => c)
				.Select(c => new CurrencyStream(c, this.Provider.RateUpdates.Where(r => r.TargetCurrency.Currency == c).Select(r => r.TargetCurrency.Value)));

			this.AllCurrencyStreamModels = new ReactiveList<CurrencyStream>(models);
			this.AllCurrencyStreams = this.AllCurrencyStreamModels.CreateDerivedCollection(m => new CurrencyViewModel(m, TimeSpan.FromSeconds(1)));

			this.RaisePropertyChanged("AllCurrencyStreams");
		}

		private void InitializeCommands()
		{
			_addActiveCurrencyCommand = new ReactiveCommand();
			_addActiveCurrencyCommand.Subscribe(currency => this.AddActiveCurrency((string)currency));

			_removeActiveCurrencyCommand = new ReactiveCommand();
			_removeActiveCurrencyCommand.Subscribe(currency => this.RemoveActiveCurrency((string)currency));
		}

		private ICurrencyRateProvider Provider { get; set; }

		public IReactiveList<string> ActiveCurrencies { get; private set; }
		public IReactiveList<string> AllCurrencies { get; private set; }

		private IReactiveList<CurrencyStream> ActiveCurrencyStreamModels { get; set; }
		public IReactiveDerivedList<CurrencyViewModel> ActiveCurrencyStreams { get; private set; }

		private IReactiveList<CurrencyStream> AllCurrencyStreamModels { get; set; }
		public IReactiveDerivedList<CurrencyViewModel> AllCurrencyStreams { get; private set; }

		private ReactiveCommand _addActiveCurrencyCommand;
		public ICommand AddActiveCurrencyCommand
		{
			get { return _addActiveCurrencyCommand; }
		}

		private ReactiveCommand _removeActiveCurrencyCommand;
		public ICommand RemoveActiveCurrencyCommand
		{
			get { return _removeActiveCurrencyCommand; }
		}

		private void AddActiveCurrency(string currency)
		{
			var existing = this.ActiveCurrencies.SingleOrDefault(x => x == currency);
			if (existing == null)
			{
				this.ActiveCurrencies.Add(currency);
			}
		}

		private void RemoveActiveCurrency(string currency)
		{
			var existing = this.ActiveCurrencies.SingleOrDefault(x => x == currency);
			if (existing != null)
			{
				this.ActiveCurrencies.Remove(currency);
			}
		}
	}

	public class CurrencyStream
	{
		public CurrencyStream(string currency, IObservable<decimal> rateUpdates)
		{
			this.Currency = currency;
			this.RateUpdates = rateUpdates;
		}

		public string Currency { get; private set; }
		public IObservable<decimal> RateUpdates { get; private set; }
	}

	public class CurrencyViewModel : ReactiveObject
	{
		public CurrencyViewModel(CurrencyStream currencyStream, TimeSpan? samplingInterval = null)
		{
			this.SamplingInterval = samplingInterval ?? TimeSpan.FromSeconds(0.25);
			this.Model = currencyStream;

			this._rate = this.Model.RateUpdates
				.Sample(this.SamplingInterval)
				.ToProperty(this, x => x.Rate);
		}

		private TimeSpan SamplingInterval { get; set; }
		private CurrencyStream Model { get; set; }

		public string Currency {
			get { return Model.Currency; }
		}

		public ObservableAsPropertyHelper<decimal> _rate;
		public decimal Rate
		{
			get { return _rate.Value; }
		}
	}
}
