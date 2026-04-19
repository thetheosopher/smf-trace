using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace SMFTrace.Wpf.ViewModels;

/// <summary>
/// View model for the About dialog.
/// </summary>
public sealed class AboutViewModel
{
    private static readonly Uri GitHubProjectUri = new("https://github.com/thetheosopher/smf-trace");
    private static readonly Uri BuyMeACoffeeUri = new("https://buymeacoffee.com/theosopher");

    public AboutViewModel()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(AboutViewModel).Assembly;

        ApplicationName = GetProductName(assembly);
        Tagline = GetDescription(assembly);
        Description = "Inspect Standard MIDI Files, validate playback behavior, and trace every event with a responsive piano roll and diagnostics view.";
        VersionText = $"Version {GetDisplayVersion(assembly)}";
        CopyrightText = GetCopyright(assembly);
        GitHubUrl = GitHubProjectUri.AbsoluteUri;
        GitHubBlurb = "Source code, releases, and issue tracking for SMF Trace.";
        BuyMeACoffeeUrl = BuyMeACoffeeUri.AbsoluteUri;
        BuyMeACoffeeBlurb = "Support future polish, maintenance, and new MIDI workflow features.";

        OpenGitHubCommand = new RelayCommand(() => OpenUrl(GitHubProjectUri));
        OpenBuyMeACoffeeCommand = new RelayCommand(() => OpenUrl(BuyMeACoffeeUri));
    }

    public string ApplicationName { get; }

    public string Tagline { get; }

    public string Description { get; }

    public string VersionText { get; }

    public string CopyrightText { get; }

    public string GitHubUrl { get; }

    public string GitHubBlurb { get; }

    public string BuyMeACoffeeUrl { get; }

    public string BuyMeACoffeeBlurb { get; }

    public ICommand OpenGitHubCommand { get; }

    public ICommand OpenBuyMeACoffeeCommand { get; }

    private static string GetProductName(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
        ?? assembly.GetName().Name
        ?? "SMF Trace";

    private static string GetDescription(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description
        ?? "See every event. Trust every tick.";

    private static string GetDisplayVersion(Assembly assembly)
    {
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion.Split('+')[0];
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    private static string GetCopyright(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright
        ?? $"© {DateTime.UtcNow.Year} Michael A. McCloskey";

    private static void OpenUrl(Uri uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri)
            {
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
            _ = MessageBox.Show(
                $"Unable to open this link right now:{Environment.NewLine}{uri.AbsoluteUri}",
                "Open Link Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
