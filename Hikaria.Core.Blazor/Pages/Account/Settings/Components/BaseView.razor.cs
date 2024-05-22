using System.Threading.Tasks;
using Hikaria.Core.Blazor.Models;
using Hikaria.Core.Blazor.Services;
using Microsoft.AspNetCore.Components;

namespace Hikaria.Core.Blazor.Pages.Account.Settings
{
    public partial class BaseView
    {
        private CurrentUser _currentUser = new CurrentUser();

        [Inject] protected IUserService UserService { get; set; }

        private void HandleFinish()
        {
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            _currentUser = await UserService.GetCurrentUserAsync();
        }
    }
}