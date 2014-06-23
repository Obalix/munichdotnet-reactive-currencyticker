using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Concurrency;

namespace GeoCoding.Views
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			var disconnectHandler = UserError.RegisterHandler(async error => {
				var msgBoxResult = await this.Dispatcher.InvokeAsync<MessageBoxResult>(() => {
					var sb = new StringBuilder();
					sb.AppendLine(error.ErrorMessage);
					sb.AppendLine();
					sb.AppendLine("Possible solution: " + error.ErrorCauseOrResolution);
					sb.AppendLine();
					sb.AppendLine("Yes = Retry, No = Abort, Cancel = Fail");

					return MessageBox.Show(this, sb.ToString(), "Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
				});

				switch (msgBoxResult)
				{
					case MessageBoxResult.Yes:
						return RecoveryOptionResult.RetryOperation;
					case MessageBoxResult.No:
						return RecoveryOptionResult.CancelOperation;
					default:
						return RecoveryOptionResult.FailOperation;
				}
			});
		}
	}
}
