﻿using Konata.Core;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using YukiChan.Core;
using YukiChan.Database.Models;
using YukiChan.Utils;

namespace YukiChan.Modules.Yuki;

public partial class YukiModule
{
    [Command("Ban",
        Command = "ban",
        Authority = YukiUserAuthority.Admin,
        Description = "关小黑屋")]
    public MessageBuilder Ban(Bot bot, MessageStruct message, string body)
    {
        var parsed = uint.TryParse(body, out var uin);

        if (parsed && Global.YukiDb.BanUser(uin))
            return new MessageBuilder()
                .Add(ReplyChain.Create(message))
                .Text($"成功把用户 {uin} 关进小黑屋啦！");

        return CommonUtils.ReplyMessage(message)
            .Text($"用户 {body} 没有找到...");
    }
}