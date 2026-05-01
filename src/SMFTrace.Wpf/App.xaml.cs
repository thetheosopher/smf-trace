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
		mainWindow.Show();

		var filePaths = GetCommandLineMidiPaths(e.Args);
		if (filePaths.Count > 0)
		{
			_ = mainWindow.OpenFilesFromCommandLineAsync(filePaths);
		}
	}

	internal static IReadOnlyList<string> GetCommandLineMidiPaths(IEnumerable<string> args)
	{
		var paths = new List<string>();
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
				paths.Add(path);
			}
		}

		return paths;
	}
}

