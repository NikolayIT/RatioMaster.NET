namespace RatioMaster.App.Services.Abstractions
{
    using System.Threading.Tasks;

    /// <summary>Copies text to the system clipboard.</summary>
    public interface IClipboardService
    {
        Task SetTextAsync(string text);
    }
}
