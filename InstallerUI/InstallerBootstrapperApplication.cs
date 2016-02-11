using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace InstallerUI
{
	public class InstallerBootstrapperApplication: BootstrapperApplication
	{
		public static Dispatcher BootstrapperDispatcher { get; private set; }
		protected override void Run()
		{
			this.Engine.Log(LogLevel.Verbose, "Launching InstallerBootstrapperApplication");
			BootstrapperDispatcher = Dispatcher.CurrentDispatcher;

			var viewModel = new MainWindowModel(this);
			viewModel.Bootstrapper.Engine.Detect();

			Util.Log("Installer running: " + this.Command.Action + " " + this.Command.Display);
			if (this.Command.Action == LaunchAction.Uninstall && this.Command.Display == Display.Embedded)
			{
				Util.Log("viewModel.Uninstall");
				viewModel.Uninstall();
				Util.Log("BootstrapperDispatcher.InvokeShutdown");
				BootstrapperDispatcher.InvokeShutdown();
			}
			else
			{
				var view = new MainWindow(viewModel);
				view.Closed += (sender, e) =>
				{
					Util.Log("BootstrapperDispatcher.InvokeShutdown");
					BootstrapperDispatcher.InvokeShutdown();
					Util.Log("Engine.Quit");
					viewModel.Dispose();
					Engine.Quit((int)ActionResult.Success);
				};
				Util.Log("view.Show");
				view.Show();
				Util.Log("Dispatcher.Run");
				Dispatcher.Run();
			}
			Util.Log("viewModel.Dispose");
			viewModel.Dispose();

			this.Engine.Quit((int)ActionResult.Success);
		}
	}
}
