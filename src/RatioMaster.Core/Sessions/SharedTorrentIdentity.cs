namespace RatioMaster.Core.Sessions
{
    using RatioMaster.Core.Abstractions;
    using RatioMaster.Core.Clients;

    internal sealed class SharedTorrentIdentity
    {
        private readonly Lock gate = new();
        private ClientIdentity current;
        private int lastKeyRotationSecond;

        public SharedTorrentIdentity(ClientIdentity identity) => this.current = identity;

        public ClientIdentity Current
        {
            get
            {
                lock (this.gate)
                {
                    return this.current;
                }
            }
        }

        public bool RefreshKeyIfDue(ClientProfile profile, IRandomSource random, int runningSeconds)
        {
            lock (this.gate)
            {
                if (this.current.Source != ClientIdentitySource.Generated || profile.KeyRefresh != KeyRefreshPolicy.Timed)
                {
                    return false;
                }

                var interval = Math.Max(1, profile.KeyRefreshMinutes) * 60;
                if (runningSeconds - this.lastKeyRotationSecond < interval)
                {
                    return false;
                }

                this.lastKeyRotationSecond = runningSeconds;
                this.current = this.current with { Key = new ClientIdentityGenerator(random).GenerateValue(profile.Key) };
                return true;
            }
        }
    }
}
