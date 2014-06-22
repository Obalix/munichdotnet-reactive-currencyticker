using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Currencies.Data
{
	public class CurrencyRate
	{
		public DateTimeOffset Timestamp { get; set; }
		public CurrencyValue BaseCurrency { get; set; }
		public CurrencyValue TargetCurrency { get; set; }

		public CurrencyRate CreateCopy()
		{
			return new CurrencyRate() {
				Timestamp = this.Timestamp,
				BaseCurrency = this.BaseCurrency.CreateCopy(),
				TargetCurrency = this.TargetCurrency.CreateCopy(),
			};
		}
	}

	public class ExtendedCurrencyRate : CurrencyRate
	{
		public string Description { get; set; }
		public string Category { get; set; }
	}
}
