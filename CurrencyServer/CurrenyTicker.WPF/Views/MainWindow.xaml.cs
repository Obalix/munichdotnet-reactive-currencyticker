using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CurrenyTicker.Views
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			var mouseDownEvents = Observable.FromEventPattern<MouseEventArgs>(this, "PreviewMouseLeftButtonDown")
				.Select(pattern => new PositionInfo { Position = pattern.EventArgs.GetPosition(this), Sender = (FrameworkElement)pattern.EventArgs.OriginalSource });
			var mouseMoveEvents = Observable.FromEventPattern<MouseEventArgs>(this, "PreviewMouseMove")
				.Select(pattern => new PositionInfo { Position = pattern.EventArgs.GetPosition(this), Sender = (FrameworkElement)pattern.EventArgs.OriginalSource });
			var mouseUpEvents = Observable.FromEventPattern<MouseEventArgs>(this, "PreviewMouseLeftButtonUp")
				.Select(pattern => new PositionInfo { Position = pattern.EventArgs.GetPosition(this), Sender = (FrameworkElement)pattern.EventArgs.OriginalSource });

			var dropEvents = Observable.FromEventPattern<DragEventArgs>(this.IcActiveCurrencies, "Drop");

			var dragEvents = mouseDownEvents
				.SelectMany(mdp => mouseMoveEvents
					.Where(mmp =>
						Math.Abs(mmp.Position.X - mdp.Position.X) > SystemParameters.MinimumHorizontalDragDistance ||
						Math.Abs(mmp.Position.Y - mdp.Position.Y) > SystemParameters.MinimumVerticalDragDistance
					)
					.StartWith(mdp)
					.TakeUntil(mouseUpEvents)
					.Skip(1)
					.Take(1)
				);

			dragEvents.Subscribe(mmp => {
				var sender = mmp.Sender;
				var dataCtx = mmp.Sender.GetValue(FrameworkElement.DataContextProperty) as ViewModels.CurrencyViewModel;

				if (dataCtx != null) 
				{
					var data = new DataObject(typeof(ViewModels.CurrencyViewModel), dataCtx);

					DragDrop.DoDragDrop(sender, data, DragDropEffects.All);
				}
			});

			dropEvents.Subscribe(e => {
				var data = e.EventArgs.Data.GetData(typeof(ViewModels.CurrencyViewModel)) as ViewModels.CurrencyViewModel;
				if (data != null)
				{
					var vm = (ViewModels.MainWindowViewModel)this.DataContext;
					vm.AddActiveCurrencyCommand.Execute(data.Currency);
				}
			});
		}

		private struct PositionInfo
		{
			public Point Position { get; set; }
			public DependencyObject Sender { get; set; }
		}
	}
}
