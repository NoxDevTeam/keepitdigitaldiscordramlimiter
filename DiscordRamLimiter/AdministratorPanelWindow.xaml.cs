using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace DiscordRamLimiter;

[SupportedOSPlatform("windows6.1")]
public partial class AdministratorPanelWindow : Window
{
    private Stream? _gifStream;
    private Drawing.Image? _gifImage;
    private Forms.PictureBox? _pictureBox;

    public AdministratorPanelWindow()
    {
        InitializeComponent();
        LoadGif();
        Closed += (_, _) => DisposeGif();
    }

    private void LoadGif()
    {
        var resource = System.Windows.Application.GetResourceStream(
            new Uri("pack://application:,,,/Assets/administrator-panel.gif", UriKind.Absolute))
            ?? throw new InvalidOperationException("The Administrator Panel GIF is missing.");

        _gifStream = resource.Stream;
        _gifImage = Drawing.Image.FromStream(_gifStream);
        _pictureBox = new Forms.PictureBox
        {
            BackColor = Drawing.Color.Black,
            Dock = Forms.DockStyle.Fill,
            Image = _gifImage,
            SizeMode = Forms.PictureBoxSizeMode.Zoom
        };

        GifHost.Child = _pictureBox;
    }

    private void DisposeGif()
    {
        GifHost.Child = null;
        _pictureBox?.Dispose();
        _gifImage?.Dispose();
        _gifStream?.Dispose();
    }
}
