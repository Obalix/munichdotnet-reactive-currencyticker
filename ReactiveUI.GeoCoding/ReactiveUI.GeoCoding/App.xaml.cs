using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;

namespace GeoCoding
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static IServiceLocator ServiceLocator { get; set; }

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			ServiceLocator = this.BootstrapServiceLocator();

			var mainWindow = ServiceLocator.GetInstance<Views.MainWindow>();
			var mainWindowVm = ServiceLocator.GetInstance<ViewModels.MainWindowViewModel>();
			mainWindow.DataContext = mainWindowVm;
			mainWindow.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);
		}

		private IServiceLocator BootstrapServiceLocator()
		{
			var container = new UnityContainer();

			container.RegisterType<Geocoding.IAsyncGeocoder, Geocoding.Google.GoogleGeocoder>();

			container.RegisterType<ViewModels.MainWindowViewModel>();
			container.RegisterType<Views.MainWindow>();

			return new UnityServiceLocator(container);
		}
	}
}
