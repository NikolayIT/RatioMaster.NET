namespace RatioMaster.Core.Sessions;

/// <summary>How the peer id, key, port and numwant are chosen for a session.</summary>
public enum IdentityMode
{
    /// <summary>Generate fresh values (copying from the running client when possible).</summary>
    Automatic,

    /// <summary>Use the explicit values in the settings.</summary>
    Custom,
}
