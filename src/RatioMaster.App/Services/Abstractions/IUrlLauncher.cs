namespace RatioMaster.App.Services.Abstractions
{
    using System.Threading.Tasks;

    /// <summary>Opens URLs and folders in the desktop's default handler.</summary>
    public interface IUrlLauncher
    {
        Task OpenUrlAsync(string url);

        Task RevealFileAsync(string path);
    }
}
