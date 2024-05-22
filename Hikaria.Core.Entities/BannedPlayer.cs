using System.ComponentModel;

namespace Hikaria.Core.Entities
{
    public class BannedPlayer
    {
        [DisplayName("SteamID")]
        public ulong SteamID { get; set; }
        [DisplayName("玩家名称")]
        public string Name { get; set; }
        [DisplayName("封禁原因")]
        public string Reason { get; set; }
        [DisplayName("封禁时间")]
        public DateTime DateBanned { get; set; }
    }
}
