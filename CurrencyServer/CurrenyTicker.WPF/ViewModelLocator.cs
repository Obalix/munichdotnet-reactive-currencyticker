using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CurrenyTicker.ViewModels;

namespace CurrenyTicker
{
	public class ViewModelLocator
	{
		public MainWindowViewModel MainWindowViewModel
		{
			get { return App.ServiceLocator.GetInstance<MainWindowViewModel>(); }
		}
	}
}
