using System.Threading.Tasks;

namespace RatioMaster.App.Services.Abstractions;

/// <summary>Copies text to the system clipboard.</summary>
public interface IClipboardService
{
    Task SetTextAsync(string text);
}
