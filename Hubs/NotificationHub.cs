using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Awai.Hubs
{
    [Authorize(Roles = "Prog,Admin,Employee")]
    public class NotificationHub : Hub
    {
        public const string StaffGroup = "staff";

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, StaffGroup);
            await base.OnConnectedAsync();
        }
    }
}
