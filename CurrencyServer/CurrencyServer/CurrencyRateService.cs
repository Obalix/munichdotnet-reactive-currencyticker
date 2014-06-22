using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Currencies;
using Currencies.Data;
using CurrencyServer.SignalRServer;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace CurrencyServer
{
	public class CurrencyRateService
	{
		private readonly static Lazy<CurrencyRateService> _instance = new Lazy<CurrencyRateService>(
			() => new CurrencyRateService(GlobalHost.ConnectionManager.GetHubContext<CurrencyProviderHub>().Clients)
		);

		public static CurrencyRateService Instance
		{
			get { return _instance.Value; }
		}

		public CurrencyRateService(IHubConnectionContext<dynamic> clients)
		{

			this.Clients = clients;
			var initialValuesUri = default(Uri);
#if (_USE_STATIC_DATA)
			var assemby = typeof(Program).Assembly;
			var assembyUri = new Uri(assemby.CodeBase);
			initialValuesUri = new Uri(assembyUri, "Resources/currency.xml");
#else
			initialValuesUri = new Uri("http://themoneyconverter.com/rss-feed/EUR/rss.xml");
#endif

			this.RateUpdatePublisher = new Subject<CurrencyRate>();
			this.Provider = new CurrencyRateProvider(initialValuesUri);
			this.Interval = TimeSpan.FromMilliseconds(0.5);

			this.RateUpdates
				.ObserveOn(ThreadPoolScheduler.Instance)
				.Subscribe(
					r => {
						NotifyClientsOfPriceUpdate(r);
					}
				);
		}

		private async void NotifyClientsOfPriceUpdate(CurrencyRate rate)
		{
			var copy = rate.CreateCopy();
			if (Clients != null)
				await this.Clients.All.UpdateCurrency(copy);
		}

		public void Start()
		{
			if (!this.IsRunning)
			{
				lock (this._isRunningLock)
				{
					if (!this.IsRunning)
					{
						this.IsRunning = true;
						this.ProviderRateUpdateSubscription = this.Provider.RateUpdates
							.ObserveOn(ThreadPoolScheduler.Instance)
							.Subscribe(
								r => this.RateUpdatePublisher.OnNext(r)
							);
						this.Provider.Start();
						this.NotifyClientsAboutUsedInterval();
					}
				}
			}
		}

		public void Stop()
		{
			if (this.IsRunning)
			{
				lock (this._isRunningLock)
				{
					if (this.IsRunning)
					{
						this.Provider.Stop();
						this.ProviderRateUpdateSubscription.Dispose();
						this.IsRunning = false;
					}
				}
			}
		}

		private CurrencyRateProvider Provider { get; set; }
		private IHubConnectionContext<dynamic> Clients { get; set; }
		private Subject<CurrencyRate> RateUpdatePublisher { get; set; }
		private IDisposable ProviderRateUpdateSubscription { get; set; }

		public object _isRunningLock = new object();
		public bool IsRunning { get; private set; }

		public IObservable<CurrencyRate> RateUpdates
		{
			get { return (IObservable<CurrencyRate>)this.RateUpdatePublisher; }
		}

		private TimeSpan _interval;
		public TimeSpan Interval
		{
			get { return _interval; }
			set
			{
				_interval = value;
				if (this.Provider != null)
				{
					this.Provider.Interval = _interval;
				}
				this.NotifyClientsAboutUsedInterval();
			}
		}

		private void NotifyClientsAboutUsedInterval()
		{
			if (this.Clients != null)
			{
				this.Clients.All.UpdateIntervall(_interval);
			}
		}

		public string[] GetAvailableCurrencies()
		{
			return this.Provider.GetAvailableCurrencies();
		}
	}
}
