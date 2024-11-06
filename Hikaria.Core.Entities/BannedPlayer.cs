using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hikaria.Core.Entities
{
    public class BannedPlayer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong SteamID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Reason { get; set; }
        [Required]
        public DateTime DateBanned { get; set; }
    }
}
