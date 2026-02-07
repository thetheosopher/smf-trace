using System.IO;
using System.Windows;

namespace SMFTrace.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		var mainWindow = new MainWindow();
		var filePath = GetCommandLineMidiPath(e.Args);
		if (!string.IsNullOrEmpty(filePath))
		{
			mainWindow.OpenFileFromCommandLine(filePath);
		}

		mainWindow.Show();
	}

	private static string? GetCommandLineMidiPath(string[] args)
	{
		foreach (var arg in args)
		{
			if (string.IsNullOrWhiteSpace(arg))
			{
				continue;
			}

			var path = arg.Trim();
			if (!File.Exists(path))
			{
				continue;
			}

			var ext = Path.GetExtension(path);
			if (ext.Equals(".mid", StringComparison.OrdinalIgnoreCase) ||
				ext.Equals(".midi", StringComparison.OrdinalIgnoreCase))
			{
				return path;
			}
		}

		return null;
	}
}

