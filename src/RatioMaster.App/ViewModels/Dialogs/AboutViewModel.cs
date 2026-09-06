using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using RatioMaster.App.Services.Abstractions;
using RatioMaster.Core.Updates;

namespace RatioMaster.App.ViewModels.Dialogs;

/// <summary>The About dialog.</summary>
public sealed partial class AboutViewModel(IUrlLauncher launcher) : DialogViewModel<bool>
{
    public string Version => AppVersion.Public;

    public string ReleaseDate => AppVersion.ReleaseDate;

    public string Runtime => $".NET {Environment.Version} on {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";

    public string Website => AppVersion.WebsiteUrl;

    public string GitHub => AppVersion.GitHubUrl;

    public string Forum => AppVersion.ForumUrl;

    public string BugReport => AppVersion.BugReportUrl;

    public string Email => AppVersion.SupportEmail;

    public string Copyright => "Copyright © 2006-2026 Nikolay Kostov. MIT licensed.";

    [RelayCommand]
    private async Task OpenAsync(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            await launcher.OpenUrlAsync(url);
        }
    }

    [RelayCommand]
    private async Task EmailAsync() => await launcher.OpenUrlAsync("mailto:" + AppVersion.SupportEmail);

    [RelayCommand]
    private async Task DonateAsync() => await launcher.OpenUrlAsync(AppVersion.DonateUrl);

    [RelayCommand]
    private void CloseDialog() => Close(true);
}
