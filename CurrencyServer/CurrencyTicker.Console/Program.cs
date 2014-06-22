using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTicker
{
	class Program
	{
		static void Main(string[] args)
		{

			var client = new Currencies.CurrencyRateProviderClient("http://localhost:8080/");

			var interval = default(TimeSpan?);

			var intervals = client.IntervalUpdates.Subscribe(iv => interval = iv);

			var allRates = client.RateUpdates.Window(TimeSpan.FromSeconds(1));
			var gbpRates = client.RateUpdates.Where(x => x.TargetCurrency.Currency == "GBP").Window(TimeSpan.FromSeconds(1));
			var screenUpdate = Observable.Zip(
				allRates.SelectMany(x => x.Count()),
				gbpRates.SelectMany(x => x.Count()),
				(lhs, rhs) => new { All = lhs, GBP = rhs, Interval = interval.GetValueOrDefault() }
			);

			Console.CursorVisible = false;
			screenUpdate.Subscribe(
				su => {
					Console.CursorLeft = 0;
					Console.CursorTop = 0;
					Console.Write("Interval: {0}", su.Interval);
					Console.CursorLeft = 0;
					Console.CursorTop = 1;
					Console.Write("Updates/sec (All): {0,8}", su.All);
					Console.CursorLeft = 0;
					Console.CursorTop = 2;
					Console.Write("Updates/sec (GBP): {0,8}", su.GBP);
				}
			);

			client.StartListening();

			Console.ReadKey();
			client.StopListening();
		}
	}
}
