using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Currencies;
using CurrenyTicker.ViewModels;
using CurrenyTicker.Views;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;

namespace CurrenyTicker
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static IServiceLocator ServiceLocator;

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			this.Start();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			this.Stop();

			base.OnExit(e);
		}

		private void Start()
		{
			ServiceLocator = this.BootstrapServiceLocator();


			var client = ServiceLocator.GetInstance<ICurrencyRateClient>();
			client.StartListening();

			Application.Current.Resources.Add("Locator", ServiceLocator.GetInstance<ViewModelLocator>());

			var mainWindow = ServiceLocator.GetInstance<MainWindow>();
			mainWindow.Show();
		}

		private void Stop()
		{
			var client = ServiceLocator.GetInstance<ICurrencyRateClient>();
			client.StopListening();
		}

		private IServiceLocator BootstrapServiceLocator()
		{
			var container = new UnityContainer();

			var baseUrl = "http://localhost:8080/";

			container.RegisterType<ViewModelLocator>(new ContainerControlledLifetimeManager());

			var client = new CurrencyRateProviderClient(baseUrl);
			container.RegisterInstance<ICurrencyRateProvider>(client, new ContainerControlledLifetimeManager());
			container.RegisterInstance<ICurrencyRateClient>(client);

			container.RegisterType<MainWindow>();
			container.RegisterType<MainWindowViewModel>();

			return new UnityServiceLocator(container);
		}
	}
}
