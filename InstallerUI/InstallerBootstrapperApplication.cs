﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace InstallerUI
{
	public class InstallerBootstrapperApplication: BootstrapperApplication
	{
		public static Dispatcher BootstrapperDispatcher { get; private set; }
		protected override void Run()
		{
			this.Engine.Log(LogLevel.Verbose, "Launching custom InstallerBootstrapperApplication");
			BootstrapperDispatcher = Dispatcher.CurrentDispatcher;

			var viewModel = new MainWindowModel(this);
			viewModel.Bootstrapper.Engine.Detect();

			var view = new MainWindow(viewModel);
			view.DataContext = viewModel;
			view.Closed += (sender, e) => BootstrapperDispatcher.InvokeShutdown();

			if (this.Command.Action == LaunchAction.Uninstall && this.Command.Display == Display.Embedded)
			{
				viewModel.Bootstrapper.Engine.Plan(LaunchAction.Uninstall);
				view.Uninstall();
				view.Close();
			}
			else
			{
				view.Show();
			}

			Dispatcher.Run();
			this.Engine.Quit(0);
		}
	}
}
