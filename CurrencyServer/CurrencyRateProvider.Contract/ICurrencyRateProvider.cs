using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Currencies.Data;

namespace Currencies
{
	public interface ICurrencyRateProvider : IDisposable
	{
		Task Start();
		Task Stop();

		Task<string[]> GetAvailableCurrencies();

		Task SetInterval(TimeSpan interval);

		IObservable<CurrencyRate> RateUpdates { get; }
		IObservable<TimeSpan> IntervalUpdates { get; }
	}
}
