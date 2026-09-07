namespace RatioMaster.App.Services.Abstractions
{
    using System;
    using System.Threading.Tasks;

    /// <summary>Marshals work onto the UI thread. Replaceable in tests.</summary>
    public interface IUiDispatcher
    {
        bool IsOnUiThread { get; }

        void Post(Action action);

        Task InvokeAsync(Action action);
    }
}
