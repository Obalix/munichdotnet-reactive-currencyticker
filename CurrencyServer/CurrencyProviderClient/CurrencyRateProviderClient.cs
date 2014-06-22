using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Currencies.Data;
using Microsoft.AspNet.SignalR.Client;

namespace Currencies
{
	public interface ICurrencyRateClient : IDisposable
	{
		void StartListening();
		void StopListening();
	}

	public class CurrencyRateProviderClient : ICurrencyRateProvider, ICurrencyRateClient
	{
		public CurrencyRateProviderClient(string baseUrl)
		{
			this.RatePublisher = new Subject<CurrencyRate>();
			this.IntervalPublisher = new Subject<TimeSpan>();
			this.BaseUrl = baseUrl;
		}

		public void Dispose()
		{
			StopListening();
		}

		private string BaseUrl { get; set; }
		private HubConnection HubConnection { get; set; }
		private IHubProxy Proxy { get; set; }

		private IDisposable Notifications { get; set; }

		private Subject<CurrencyRate> RatePublisher { get; set; }
		private Subject<TimeSpan> IntervalPublisher { get; set; }

		public async void StartListening()
		{
			this.HubConnection = new HubConnection(this.BaseUrl);
			this.Proxy = this.HubConnection.CreateHubProxy("CurrencyProviderHub");

			this.Notifications = new CompositeDisposable() {
				this.Proxy.On<CurrencyRate>("UpdateCurrency", this.OnCurrencyRateUpdate),
				this.Proxy.On<TimeSpan>("UpdateIntervall", this.OnIntervalUpdate),
			};

			ServicePointManager.DefaultConnectionLimit = 10;

			await this.HubConnection.Start(new Microsoft.AspNet.SignalR.Client.Transports.LongPollingTransport());
		}

		public void StopListening()
		{
			if (this.HubConnection != null)
			{
				this.HubConnection.Dispose();
			}

			if (Notifications != null)
			{
				this.Notifications.Dispose();
			}

			this.Notifications = null;
			this.HubConnection = null;
			this.Proxy = null;
		}

		public Task Start()
		{
			return this.Proxy.Invoke("Start");
		}

		public Task Stop()
		{
			return this.Proxy.Invoke("Stop");
		}

		public Task SetInterval(TimeSpan interval)
		{
			return this.Proxy.Invoke("SetInterval", interval);
		}

		public IObservable<Data.CurrencyRate> RateUpdates
		{
			get { return (IObservable<CurrencyRate>)this.RatePublisher; }
		}

		public IObservable<TimeSpan> IntervalUpdates
		{
			get { return (IObservable<TimeSpan>)this.IntervalPublisher; }
		}

		public Task<string[]> GetAvailableCurrencies()
		{
			return this.Proxy.Invoke<string[]>("GetAvailableCurrencies");
		}

		private void OnCurrencyRateUpdate(CurrencyRate rate)
		{
			this.RatePublisher.OnNext(rate);
		}

		private void OnIntervalUpdate(TimeSpan interval)
		{
			this.IntervalPublisher.OnNext(interval);
		}
	}
}
