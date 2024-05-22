using System.Threading.Tasks;
using Hikaria.Core.Blazor.Models;
using Hikaria.Core.Blazor.Services;
using Microsoft.AspNetCore.Components;

namespace Hikaria.Core.Blazor.Pages.Profile
{
    public partial class Basic
    {
        private BasicProfileDataType _data = new BasicProfileDataType();

        [Inject] protected IProfileService ProfileService { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            _data = await ProfileService.GetBasicAsync();
        }
    }
}