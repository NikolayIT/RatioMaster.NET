using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using RatioMaster.App.Services.Abstractions;
using RatioMaster.Core.Updates;

namespace RatioMaster.App.ViewModels.Dialogs;

/// <summary>What the user chose in the "new version" dialog.</summary>
public enum NewVersionChoice
{
    Later,
    Download,
    Skip,
}

/// <summary>Tells the user a newer build is available.</summary>
public sealed partial class NewVersionViewModel(string remoteVersion, IUrlLauncher launcher)
    : DialogViewModel<NewVersionChoice>
{
    public string Message =>
        $"A new version of RatioMaster.NET is available (build {remoteVersion}). You are running {AppVersion.Public}.";

    public string Website => AppVersion.WebsiteUrl;

    [RelayCommand]
    private async Task DownloadAsync()
    {
        await launcher.OpenUrlAsync(AppVersion.WebsiteUrl);
        this.Close(NewVersionChoice.Download);
    }

    [RelayCommand]
    private void Later() => this.Close(NewVersionChoice.Later);

    [RelayCommand]
    private void Skip() => this.Close(NewVersionChoice.Skip);
}
