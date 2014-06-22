using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Currencies.Data;
using Microsoft.AspNet.SignalR;
using Owin;

namespace CurrencyServer.SignalRServer
{
	public class CurrencyProviderHub : Hub
	{
		public void SetInterval(TimeSpan interval)
		{
			CurrencyRateService.Instance.Interval = interval;
		}

		public void Start()
		{
			CurrencyRateService.Instance.Start();
		}

		public void Stop()
		{
			CurrencyRateService.Instance.Stop();
		}

		public string[] GetAvailableCurrencies()
		{
			return CurrencyRateService.Instance.GetAvailableCurrencies();
		}
	}

	internal class StartUp
	{
		public void Configuration(IAppBuilder app)
		{
			app.Map("/signalr", map => {
				var hubConfiguration = new HubConfiguration() {
					EnableDetailedErrors = true,
				};

				map.RunSignalR(hubConfiguration);
			});
		}
	}
}
