using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using DiscordRamLimiter.Models;
using DiscordRamLimiter.Services;

namespace DiscordRamLimiter;

public partial class UpdateWindow : Window
{
    private readonly UpdateService _updateService;
    private readonly UpdateCheckResult _update;
    private readonly CancellationTokenSource _downloadCancellation = new();
    private bool _isInstalling;

    public UpdateWindow(UpdateService updateService, UpdateCheckResult update)
    {
        _updateService = updateService;
        _update = update;

        InitializeComponent();

        VersionText.Text = $"Version {FormatVersion(update.LatestVersion)} is available (installed: {FormatVersion(update.CurrentVersion)}).";
        ChangelogText.Text = update.Changelog;
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        InstallButton.IsEnabled = false;
        LaterButton.IsEnabled = false;
        DownloadProgressBar.Visibility = Visibility.Visible;
        DownloadProgressBar.IsIndeterminate = true;
        DownloadStatusText.Text = "Downloading the update…";

        try
        {
            var progress = new Progress<UpdateDownloadProgress>(UpdateProgress);
            var installerPath = await _updateService.DownloadAndVerifyAsync(
                _update,
                progress,
                _downloadCancellation.Token);

            DownloadProgressBar.IsIndeterminate = false;
            DownloadProgressBar.Value = 100;
            DownloadStatusText.Text = "Verified. Installing and restarting…";

            _isInstalling = true;
            UpdateService.StartInstaller(installerPath);
            System.Windows.Application.Current.Shutdown();
        }
        catch (OperationCanceledException)
        {
            DownloadStatusText.Text = "Update download cancelled.";
            ResetButtons();
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or UnauthorizedAccessException or InvalidDataException or System.Security.SecurityException or InvalidOperationException)
        {
            DownloadStatusText.Text = $"Update failed safely: {exception.Message}";
            ResetButtons();
        }
    }

    private void UpdateProgress(UpdateDownloadProgress progress)
    {
        if (progress.Percentage is not { } percentage)
        {
            DownloadProgressBar.IsIndeterminate = true;
            DownloadStatusText.Text = $"Downloaded {FormatBytes(progress.BytesReceived)}…";
            return;
        }

        DownloadProgressBar.IsIndeterminate = false;
        DownloadProgressBar.Value = percentage;
        DownloadStatusText.Text = $"Downloaded {percentage:0}% ({FormatBytes(progress.BytesReceived)} of {FormatBytes(progress.TotalBytes!.Value)}).";
    }

    private void ResetButtons()
    {
        InstallButton.IsEnabled = true;
        LaterButton.IsEnabled = true;
        DownloadProgressBar.IsIndeterminate = false;
    }

    private void LaterButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_isInstalling)
        {
            _downloadCancellation.Cancel();
        }
    }

    private static string FormatVersion(Version version)
    {
        return version.Build >= 0 ? version.ToString(3) : version.ToString();
    }

    private static string FormatBytes(long bytes)
    {
        return bytes >= 1024L * 1024L
            ? $"{bytes / 1024d / 1024d:0.0} MB"
            : $"{bytes / 1024d:0.0} KB";
    }
}
