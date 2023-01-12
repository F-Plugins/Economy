using System.Drawing;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Permissions;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Unturned.Commands;

namespace Economy.Commands;

[Command("balance", Priority = Priority.High)]
[CommandAlias("bal")]
[CommandDescription("Displays your current balance")]
[CommandSyntax("<player>")]
[RegisterCommandPermission(BalanceViewOthers, Description = "Allows users to view owner users balance")]
public class CBalance : UnturnedCommand
{
    private const string BalanceViewOthers = "others";

    private readonly IEconomyProvider _economyProvider;
    private readonly IStringLocalizer _stringLocalizer;

    public CBalance(
        IServiceProvider serviceProvider,
        IEconomyProvider economyProvider,
        IStringLocalizer stringLocalizer) : base(serviceProvider)
    {
        _economyProvider = economyProvider;
        _stringLocalizer = stringLocalizer;
    }

    protected override async UniTask OnExecuteAsync()
    {
        var target = Context.Actor;

        if (Context.Parameters.Length > 0)
        {
            if (await CheckPermissionAsync(BalanceViewOthers) != PermissionGrantResult.Grant)
                throw new NotEnoughPermissionException(Context, BalanceViewOthers);

            if (!Context.Parameters.TryGet<IUser>(0, out var targetUser))
            {
                throw new UserFriendlyException(_stringLocalizer["commands:errors:user_not_found",
                    new { Query = Context.Parameters[0] }]);
            }

            target = targetUser!;
        }

        var balance = await _economyProvider.GetBalanceAsync(target.Id, target.Type);
        
        var tanslationKey = target.Id == Context.Actor.Id ? "commands:success:balance_display" : "commands:success:balance_display_other";

        await PrintAsync(_stringLocalizer[tanslationKey, new
        {
            User = target,
            _economyProvider.CurrencyName,
            _economyProvider.CurrencySymbol,
            Balance = balance
        }], Color.LawnGreen);
    }
}