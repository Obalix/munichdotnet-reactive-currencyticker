using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Currencies;
using Microsoft.Owin.Hosting;

namespace CurrencyServer
{
	class Program
	{
		static void Main(string[] args)
		{
			var url = "http://localhost:8080/";
			using (WebApp.Start<CurrencyServer.SignalRServer.StartUp>(url)) {
				Console.CursorVisible = false;
				Console.CursorLeft = 0;
				Console.CursorTop = 0;
				Console.Write("Server started at {0}", url);

				var allRates = CurrencyRateService.Instance.RateUpdates.Window(TimeSpan.FromSeconds(1));
				var gbpRates = CurrencyRateService.Instance.RateUpdates.Where(x => x.TargetCurrency.Currency == "GBP").Window(TimeSpan.FromSeconds(1));
				var screenRefresh = Observable.Zip(
					allRates.SelectMany(w => w.Count()),
					gbpRates.SelectMany(w => w.Count()),
					(lhs, rhs) => new { All = lhs, Gbp = rhs }
				);

				var subRefresh = screenRefresh.Subscribe(
					u => {
						Console.CursorLeft = 0;
						Console.CursorTop = 1;
						Console.Write("{0,10:D} updates/sec", u.All);

						Console.CursorLeft = 0;
						Console.CursorTop = 2;
						Console.Write("{0,10:D} updates/sec (GBP)", u.Gbp);
					}
				);

				CurrencyRateService.Instance.Start();

				Console.ReadKey();
				CurrencyRateService.Instance.Stop();
				subRefresh.Dispose();
			}


//			var initialValuesUri = default(Uri);
//#if (_USE_STATIC_DATA)
//			var assemby = typeof(Program).Assembly;
//			var assembyUri = new Uri(assemby.CodeBase);
//			initialValuesUri = new Uri(assembyUri, "Resources/currency.xml");
//#else
//			initialValuesUri = new Uri("http://themoneyconverter.com/rss-feed/EUR/rss.xml");
//#endif
//			var service = new CurrencyRateProvider(initialValuesUri);
//			service.Interval = TimeSpan.FromMilliseconds(0.05);
//			service.Start();

//			var allRates = service.RateUpdates.Window(TimeSpan.FromSeconds(1));
//			var gbpRates = service.RateUpdates.Where(x => x.TargetCurrency.Currency == "GBP").Window(TimeSpan.FromSeconds(1));

//			var screenUpdate = Observable.Zip(
//				allRates.SelectMany(w => w.Count()),
//				gbpRates.SelectMany(w => w.Count()),
//				(lhs, rhs) => new { All = lhs, Gbp = rhs }
//			);

//			//service.RateUpdates
//			//	//.Where(x => x.TargetCurrency.Currency == "GBP")
//			//	.Subscribe(
//			//		r => Console.WriteLine("{0} {1} = {2} {3}", r.BaseCurrency.Value, r.BaseCurrency.Currency, r.TargetCurrency.Value, r.TargetCurrency.Currency)
//			//	);

//			//allRates
//			//	.SelectMany(x => x.Count())
//			//	.Subscribe(
//			//		c => {
//			//			Console.CursorLeft = 0;
//			//			Console.CursorTop = 0;
//			//			Console.WriteLine("{0,10:D} updates/sec", c);
//			//		}
//			//	);

//			//gbpRates
//			//	.SelectMany(x => x.Count())
//			//	.Subscribe(
//			//		c => {
//			//			Console.CursorLeft = 0;
//			//			Console.CursorTop = 1;
//			//			Console.WriteLine("{0,10:D} updates/sec (GBP)", c);
//			//		}
//			//	);

//			screenUpdate.Subscribe(
//				u => {
//					Console.CursorVisible = false;
//					Console.CursorLeft = 0;
//					Console.CursorTop = 0;
//					Console.WriteLine("{0,10:D} updates/sec", u.All);
//					Console.CursorLeft = 0;
//					Console.CursorTop = 1;
//					Console.WriteLine("{0,10:D} updates/sec (GBP)", u.Gbp);
//				}
//			);
	
//			Console.ReadKey();
//			service.Stop();
		}
	}
}
