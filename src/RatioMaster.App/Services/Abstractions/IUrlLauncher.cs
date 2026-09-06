using System.Threading.Tasks;

namespace RatioMaster.App.Services.Abstractions;

/// <summary>Opens URLs and folders in the desktop's default handler.</summary>
public interface IUrlLauncher
{
    Task OpenUrlAsync(string url);

    Task RevealFileAsync(string path);
}
