using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;


namespace Petcord
{
    //custom attribute for commands to require admin role
    //or be the bot maintainer
    public class RequireAdminRole : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var config = services.GetRequiredService<Functions.ConfigFile>();

            if (context.User.Id == config.MaintainerId)
                return Task.FromResult(PreconditionResult.FromSuccess());

            //only guild users can have roles
            if (!(context.User is SocketGuildUser gUser)) 
                return Task.FromResult(PreconditionResult.FromError("Private context"));

            return Task.FromResult(gUser.Roles.Any(r => r.Id == config.AdminRoleId) ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("No admin role"));
        }
    }
}