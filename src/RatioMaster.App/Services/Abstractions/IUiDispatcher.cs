using System;
using System.Threading.Tasks;

namespace RatioMaster.App.Services.Abstractions;

/// <summary>Marshals work onto the UI thread. Replaceable in tests.</summary>
public interface IUiDispatcher
{
    bool IsOnUiThread { get; }

    void Post(Action action);

    Task InvokeAsync(Action action);
}
