#if false

using TheArchive.Core.FeaturesAPI;

namespace Hikaria.Core.Features;

internal class PlayerPing : Feature
{
    public override string Name => "Player Ping";

    public struct pPing
    {
        public int GetPing()
        {
            return Math.Min(999, DateTime.UtcNow.Millisecond - sendTime);
        }

        public int sendTime;
    }
}

#endif