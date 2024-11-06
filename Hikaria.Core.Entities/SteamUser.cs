using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hikaria.Core.Entities
{
    public class SteamUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong SteamID { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public UserPrivilege Privilege { get; set; }
    }

    [Flags]
    public enum UserPrivilege : ulong
    {
        None = 0UL,
        BanPlayer = 1UL << 0,


        Admin = ulong.MaxValue
    }
}
