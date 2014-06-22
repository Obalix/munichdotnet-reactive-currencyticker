using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Currencies.Data
{
	public class CurrencyValue
	{
		public decimal Value { get; set; }
		public string Currency { get; set; }

		public CurrencyValue CreateCopy()
		{
			return new CurrencyValue() { Value = this.Value, Currency = this.Currency };
		}
	}
}
