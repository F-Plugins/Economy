using System.Drawing;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Permissions;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Permissions;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Unturned.Commands;

namespace Economy.Commands;

[Command("pay")]
[CommandDescription("Pay currency to other user")]
[CommandSyntax("<user> <amount>")]
[RegisterCommandPermission(PaySelf, Description = "Allows paying yourself")]
[RegisterCommandPermission(PayNegative, Description = "Allows paying negative balance to other players")]
[RegisterCommandPermission(InfiniteBalance, Description = "Allows paying without limits")]
public class CPay : UnturnedCommand
{
    private const string PaySelf = "self";
    private const string PayNegative = "negative";
    private const string InfiniteBalance = "infinite";

    private readonly IStringLocalizer _stringLocalizer;
    private readonly IEconomyProvider _economyProvider;

    public CPay(
        IServiceProvider serviceProvider,
        IStringLocalizer stringLocalizer,
        IEconomyProvider economyProvider) : base(serviceProvider)
    {
        _stringLocalizer = stringLocalizer;
        _economyProvider = economyProvider;
    }

    protected override async UniTask OnExecuteAsync()
    {
        if (Context.Parameters.Length < 2)
            throw new CommandWrongUsageException(Context);

        if (!Context.Parameters.TryGet<IUser>(0, out var target))
            throw new UserFriendlyException(_stringLocalizer["commands:errors:user_not_found",
                new { Query = Context.Parameters[0] }]);

        var payingHimself = target!.Id == Context.Actor.Id;

        if (payingHimself && await CheckPermissionAsync(PaySelf) != PermissionGrantResult.Grant)
            throw new UserFriendlyException(_stringLocalizer["commands:errors:pay_self"]);

        if (!Context.Parameters.TryGet(1, out decimal amount))
            throw new CommandWrongUsageException(Context);

        var negativeAmount = amount < 0;

        if (negativeAmount && await CheckPermissionAsync(PayNegative) != PermissionGrantResult.Grant)
            throw new UserFriendlyException(_stringLocalizer["commands:errors:pay_negative"]);

        var reason = Context.Parameters.Length > 2 ? Context.Parameters.GetArgumentLine(2) : null;

        var infiniteAccount = await CheckPermissionAsync(InfiniteBalance) == PermissionGrantResult.Grant;

        if (payingHimself)
        {
            try
            {
                await _economyProvider.UpdateBalanceAsync(target.Id, target.Type, amount, reason);
            }
            catch (NotEnoughBalanceException balanceException)
            {
                await _economyProvider.SetBalanceAsync(target.Id, target.Type, 0);
                amount = balanceException.Balance ?? amount;
            }
            await PrintAsync(
                _stringLocalizer[negativeAmount ? "commands:success:burn" : "commands:success:print", new
                {
                    _economyProvider.CurrencySymbol,
                    _economyProvider.CurrencyName,
                    Amount = amount,
                    Reason = reason,
                    Target = target,
                    Sender = Context.Actor,
                    PayingSelf = true
                }],
                Color.LawnGreen);
            return;
        }

        var sendTranslationKey = "commands:success:payment:sent";

        if (!infiniteAccount)
        {
            await _economyProvider.UpdateBalanceAsync(Context.Actor.Id, Context.Actor.Type,
                negativeAmount ? amount : -amount, reason);
        }

        if (infiniteAccount && !negativeAmount)
        {
            sendTranslationKey = "commands:success:print";
        }
        else if (negativeAmount)
        {
            sendTranslationKey = "commands:success:withdraw:sent";
        }

        var receiveTranslationKey = negativeAmount ? "commands:success:withdraw:receive" : "commands:success:payment:receive";

        try
        {
            await _economyProvider.UpdateBalanceAsync(target.Id, target.Type, amount, reason);
        }
        catch (NotEnoughBalanceException balanceException)
        {
            await _economyProvider.SetBalanceAsync(target.Id, target.Type, 0);
            amount = balanceException.Balance ?? amount;
        }

        var translationArgs = new
        {
            _economyProvider.CurrencySymbol,
            _economyProvider.CurrencyName,
            Amount = amount,
            Reason = reason,
            Target = target,
            Sender = Context.Actor,
            PayingSelf = false
        };

        await PrintAsync(_stringLocalizer[sendTranslationKey, translationArgs], Color.LawnGreen);
        await target.PrintMessageAsync(_stringLocalizer[receiveTranslationKey, translationArgs], Color.LawnGreen);
    }
}