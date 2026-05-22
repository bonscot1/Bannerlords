namespace Bannerlords.Coop.Network.Session
{
    public enum CoopRole
    {
        /// <summary>Not in a session.</summary>
        None,
        /// <summary>Hosts the authoritative campaign state.</summary>
        Host,
        /// <summary>Connected to a host as a guest.</summary>
        Client,
    }
}
