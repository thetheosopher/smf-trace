using System.Windows;
using System.Windows.Media;
using SMFTrace.Wpf.ViewModels;

namespace SMFTrace.Wpf.Views;

/// <summary>
/// Interaction logic for AboutWindow.xaml
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow(AboutViewModel viewModel)
        : this(viewModel, true)
    {
    }

    public AboutWindow(AboutViewModel viewModel, bool isDarkTheme)
    {
        InitializeComponent();
        DataContext = viewModel;
        ApplyThemeResources(isDarkTheme);
    }

    private void ApplyThemeResources(bool isDarkTheme)
    {
        if (isDarkTheme)
        {
            SetBrushResource("DialogWindowBrush", "#1E1E23");
            SetBrushResource("DialogSurfaceBrush", "#171B22");
            SetGradientResource("HeroBackgroundBrush", "#0F172A", "#123E7A", "#1D78C8");
            SetBrushResource("HeroTitleBrush", "#FFFFFF");
            SetBrushResource("HeroSubtitleBrush", "#D8E8FF");
            SetBrushResource("HeroChipBrush", "#20FFFFFF");
            SetBrushResource("HeroChipTextBrush", "#FFFFFF");
            SetBrushResource("HeroIconPlateBrush", "#26FFFFFF");
            SetBrushResource("HeroIconBorderBrush", "#66D5EBFF");
            SetBrushResource("BodyTextBrush", "#D5E0EE");
            SetBrushResource("InfoCardBrush", "#202631");
            SetBrushResource("InfoCardBorderBrush", "#344052");
            SetBrushResource("SectionLabelBrush", "#9AAEC8");
            SetBrushResource("InfoPrimaryBrush", "#F8FAFC");
            SetBrushResource("CardButtonBackgroundBrush", "#252D38");
            SetBrushResource("CardButtonBorderBrush", "#3A4558");
            SetBrushResource("CardButtonHoverBackgroundBrush", "#2D3745");
            SetBrushResource("CardButtonHoverBorderBrush", "#7FB6FF");
            SetBrushResource("CardButtonPressedBackgroundBrush", "#193B62");
            SetBrushResource("CardButtonPressedBorderBrush", "#4B8EE6");
            SetBrushResource("CardTitleBrush", "#FFFFFF");
            SetBrushResource("CardTextBrush", "#B7C7DC");
            SetBrushResource("GitHubLinkBrush", "#7FCCFF");
            SetBrushResource("SupportLinkBrush", "#FFC36E");
            SetBrushResource("FooterBrush", "#151A22");
            SetBrushResource("FooterTextBrush", "#8EA0B9");
            SetBrushResource("CloseButtonBrush", "#007ACC");
            SetBrushResource("CloseButtonHoverBrush", "#0E87DE");
            SetBrushResource("CloseButtonPressedBrush", "#0065A8");
            SetBrushResource("CloseButtonForegroundBrush", "#FFFFFF");
        }
        else
        {
            SetBrushResource("DialogWindowBrush", "#E8EEF7");
            SetBrushResource("DialogSurfaceBrush", "#F3F7FC");
            SetGradientResource("HeroBackgroundBrush", "#FFFFFF", "#DCEEFF", "#A8D4FF");
            SetBrushResource("HeroTitleBrush", "#0F172A");
            SetBrushResource("HeroSubtitleBrush", "#35506B");
            SetBrushResource("HeroChipBrush", "#BFE4F2FF");
            SetBrushResource("HeroChipTextBrush", "#17324B");
            SetBrushResource("HeroIconPlateBrush", "#F8FBFF");
            SetBrushResource("HeroIconBorderBrush", "#9DC9F1");
            SetBrushResource("BodyTextBrush", "#243247");
            SetBrushResource("InfoCardBrush", "#FFFFFF");
            SetBrushResource("InfoCardBorderBrush", "#D5E1EF");
            SetBrushResource("SectionLabelBrush", "#4C5D78");
            SetBrushResource("InfoPrimaryBrush", "#111827");
            SetBrushResource("CardButtonBackgroundBrush", "#FFFFFF");
            SetBrushResource("CardButtonBorderBrush", "#D5E1EF");
            SetBrushResource("CardButtonHoverBackgroundBrush", "#F6FAFF");
            SetBrushResource("CardButtonHoverBorderBrush", "#7FB6FF");
            SetBrushResource("CardButtonPressedBackgroundBrush", "#EAF3FF");
            SetBrushResource("CardButtonPressedBorderBrush", "#4B8EE6");
            SetBrushResource("CardTitleBrush", "#111827");
            SetBrushResource("CardTextBrush", "#4B5D78");
            SetBrushResource("GitHubLinkBrush", "#1D78C8");
            SetBrushResource("SupportLinkBrush", "#B35A00");
            SetBrushResource("FooterBrush", "#E8EFF8");
            SetBrushResource("FooterTextBrush", "#617189");
            SetBrushResource("CloseButtonBrush", "#2F7FD8");
            SetBrushResource("CloseButtonHoverBrush", "#2472C6");
            SetBrushResource("CloseButtonPressedBrush", "#11558F");
            SetBrushResource("CloseButtonForegroundBrush", "#FFFFFF");
        }
    }

    private void SetBrushResource(string key, string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        Resources[key] = brush;
    }

    private void SetGradientResource(string key, string startHexColor, string middleHexColor, string endHexColor)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };

        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(startHexColor), 0.0));
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(middleHexColor), 0.6));
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(endHexColor), 1.0));
        brush.Freeze();

        Resources[key] = brush;
    }
}
