using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Currencies.Data;

namespace Currencies
{
	public class CurrencyRateProvider : ICurrencyRateProvider
	{
		// singalr cannot deal with messages at a shorter interval, without causing a memory leak.
		private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(0.5);

		private static readonly Regex RE_CurrencyTitle = new Regex(@"^(?<currency>[A-Z]{3})\/(?<base>[A-Z]{3})$");
		private static readonly Regex RE_Description = new Regex(@"^(?<baseval>[0-9.,]+)\s+[^=]+=\s+(?<currval>[0-9.,]+)\s+.+$");

		public CurrencyRateProvider(Uri source)
		{
			this.RatePublisher = new Subject<CurrencyRate>();
			this.IntervalPublisher = new Subject<TimeSpan>();

			this.Randomizer = new Lazy<Random>();
			this.Interval = TimeSpan.FromMilliseconds(10);
			this.Source = source;

			this.CurrencyRates = new BlockingCollection<ExtendedCurrencyRate>();
			this.Parse(this.Source)
				.Do(item => this.CurrencyRates.Add(item))
				.LastOrDefaultAsync()
				.Wait();
		}

		public void Dispose()
		{
			this.Stop();
		}
		
		public Uri Source { get; private set; }
		public BlockingCollection<ExtendedCurrencyRate> CurrencyRates { get; private set; }
		private IDisposable TimerSubscription { get; set; }
		private Lazy<Random> Randomizer { get; set; }

		private TimeSpan _interval;
		public TimeSpan Interval
		{
			get { return _interval; }
			set
			{
				if (_interval != value)
				{
					this._interval = value;
					this.IntervalPublisher.OnNext(_interval);
					if (this.TimerSubscription != null)
					{
						this.Stop();
						this.Start();
					}
				}
			}
		}

		private Subject<CurrencyRate> RatePublisher { get; set; }

		public IObservable<CurrencyRate> RateUpdates
		{
			get
			{
				return (IObservable<CurrencyRate>)this.RatePublisher;
			}
		}

		private Subject<TimeSpan> IntervalPublisher { get; set; }

		public IObservable<TimeSpan> IntervalUpdates {
			get
			{
				return (IObservable<TimeSpan>)this.IntervalPublisher;
			}
		}

		public void Start()
		{
			this.CurrencyRates
				.Select(x => x.CreateCopy())
				.ToList()
				.ForEach(x => this.RatePublisher.OnNext(x));

			this.TimerSubscription = Observable.Interval(this.Interval, NewThreadScheduler.Default)
				.Timestamp()
				.Subscribe(
					(t) => {
						var curPos = this.Randomizer.Value.Next(0, this.CurrencyRates.Count);
						var item = this.CurrencyRates.ElementAt(curPos);
						var rateInfo = default(CurrencyRate);

						lock (item)
						{
							var max = item.TargetCurrency.Value * 0.01m;
							var change = (((decimal)this.Randomizer.Value.NextDouble()) - 0.5m) * max * 2m;
							item.TargetCurrency.Value += change;
							rateInfo = item.CreateCopy();
						}

						this.RatePublisher.OnNext(rateInfo);
					}
				);
		}

		public void Stop()
		{
			if (this.TimerSubscription != null)
			{
				this.TimerSubscription.Dispose();
				this.TimerSubscription = null;
			}
		}

		public string[] GetAvailableCurrencies()
		{
			return this.CurrencyRates.Select(x => x.TargetCurrency.Currency).ToArray();
		}

		private IObservable<ExtendedCurrencyRate> Parse(Uri source)
		{
			return Observable.Create<ExtendedCurrencyRate>(o => {
				return TaskPoolScheduler.Default.Schedule(() => {
					try
					{
						var reader = XmlReader.Create(source.ToString());
						var feed = SyndicationFeed.Load(reader);
						reader.Close();
						var items = feed.Items.ToList();

						Parallel.ForEach(items, (item) => {
							var rate = this.ParseCurrencyRate(item);
							o.OnNext(rate);
						});

						o.OnCompleted();
					}
					catch (Exception ex)
					{
						o.OnError(ex);
					}
				});
			});
		}

		public ExtendedCurrencyRate ParseCurrencyRate(SyndicationItem item)
		{
			try
			{
				var matchTitle = RE_CurrencyTitle.Match(item.Title.Text);
				var baseCurrency = matchTitle.Groups["base"].Value;
				var targetCurrency = matchTitle.Groups["currency"].Value;

				var matchDesc = RE_Description.Match(item.Summary.Text);
				var baseRate = decimal.Parse(matchDesc.Groups["baseval"].Value, CultureInfo.InvariantCulture);
				var targetRate = decimal.Parse(matchDesc.Groups["currval"].Value, CultureInfo.InvariantCulture);

				return new ExtendedCurrencyRate() {
					BaseCurrency = new CurrencyValue() { Value = baseRate, Currency = baseCurrency },
					TargetCurrency = new CurrencyValue() { Value = targetRate, Currency = targetCurrency },
					Timestamp = item.PublishDate,
					Description = item.Summary.Text,
					Category = item.Categories.Select(x => x.Name).FirstOrDefault(),
				};
			}
			catch
			{
				throw;
			}
		}

		Task ICurrencyRateProvider.Start()
		{
			return Task.Factory.StartNew(() => {
				this.Start();
			});
		}

		Task ICurrencyRateProvider.Stop()
		{
			return Task.Factory.StartNew(() => {
				this.Stop();
			});
		}

		Task<string[]> ICurrencyRateProvider.GetAvailableCurrencies()
		{
			return Task.Factory.StartNew<string[]>(() => {
				return this.GetAvailableCurrencies();
			});
		}

		Task ICurrencyRateProvider.SetInterval(TimeSpan interval)
		{
			return Task.Factory.StartNew(() => {
				interval = (interval < MinInterval ? MinInterval : interval);
				this.Interval = interval;
			});
		}

		IObservable<TimeSpan> ICurrencyRateProvider.IntervalUpdates
		{
			get { return this.IntervalUpdates; }
		}

		IObservable<CurrencyRate> ICurrencyRateProvider.RateUpdates
		{
			get { return this.RateUpdates; }
		}

		void IDisposable.Dispose()
		{
			this.Dispose();
		}
	}
}
