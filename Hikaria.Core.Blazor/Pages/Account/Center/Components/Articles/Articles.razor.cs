using System.Collections.Generic;
using Hikaria.Core.Blazor.Models;
using Microsoft.AspNetCore.Components;

namespace Hikaria.Core.Blazor.Pages.Account.Center
{
    public partial class Articles
    {
        [Parameter] public IList<ListItemDataType> List { get; set; }
    }
}