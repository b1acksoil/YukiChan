using Genbox.WolframAlpha;
using Genbox.WolframAlpha.Responses;
using Konata.Core;
using Konata.Core.Message;
using SkiaSharp;
using YukiChan.Core;
using YukiChan.Utils;

namespace YukiChan.Modules;

[Module("WolframAlpha",
    Command = "wa",
    Description = "Wolfram Alpha 搜索",
    Version = "1.0.0")]
public class WolframAlphaModule : ModuleBase
{
    private static readonly ModuleLogger Logger = new("WolframAlpha");

    [Command("WolframAlpha",
        Description = "Wolfram Alpha 搜索",
        Usage = "wa <搜索内容>")]
    public static async Task<MessageBuilder> GetWolframAlphaResult(Bot bot, MessageStruct message, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return message.Reply("请输入需要搜索的内容哦~");

        var client = new WolframAlphaClient(Global.YukiConfig.WolframAlpha.AppId);

        var results = await client.FullResultAsync(body);

        Logger.Debug("Searching Timing: " + results.Timing);

        var image = await GenerateImage(results);

        return new MessageBuilder().Image(image);
    }

    private static async Task<byte[]> GenerateImage(FullResultResponse result)
    {
        return await Task.Run(() =>
        {
            var totalHeight = 0;
            var maxWidth = 500;
            var imageTasks = new List<Task>();
            var images = new Dictionary<int, byte[]>();

            foreach (var pod in result.Pods)
            {
                totalHeight += 30;
                foreach (var subPod in pod.SubPods)
                {
                    var y = totalHeight;
                    var task = Task.Run(() =>
                    {
                        var image = NetUtils.DownloadBytes(subPod.Image.Src).Result;
                        images[y] = image;
                        Logger.Debug($"Downloaded image: {subPod.Image.Src}");
                    });
                    imageTasks.Add(task);

                    totalHeight += subPod.Image.Height + 10;
                    if (subPod.Image.Width > maxWidth)
                        maxWidth = subPod.Image.Width + 50;
                }
            }

            totalHeight += 30;

            Task.WaitAll(imageTasks.ToArray());

            var imageInfo = new SKImageInfo(maxWidth, totalHeight);
            using var surface = SKSurface.Create(imageInfo);
            var canvas = surface.Canvas;

            using var fontBold = SKTypeface.FromFile("Assets/Fonts/TitilliumWeb-SemiBold.ttf");

            using var backgroundPaint = new SKPaint
            {
                Color = SKColors.White
            };
            using var titlePaint = new SKPaint
            {
                Color = SKColor.Parse("#f96932"),
                TextSize = 18,
                IsAntialias = true,
                Typeface = fontBold
            };
            using var rectPaint = new SKPaint
            {
                Color = SKColor.Parse("e2e2e2")
            };

            canvas.DrawRect(0, 0, imageInfo.Width, totalHeight, backgroundPaint);

            var yPosition = 0;

            foreach (var pod in result.Pods)
            {
                canvas.DrawRect(0, yPosition, imageInfo.Width, 30, rectPaint);
                canvas.DrawText(pod.Title, 8, yPosition + 20, titlePaint);
                yPosition += 30;

                foreach (var subPod in pod.SubPods)
                    yPosition += subPod.Image.Height + 10;
            }

            foreach (var (y, image) in images)
            {
                using var imageBitmap = SKBitmap.Decode(image);
                canvas.DrawBitmap(imageBitmap, 16, y + 5);
            }

            titlePaint.Color = SKColor.Parse("#dd0e00");
            canvas.DrawText("by YukiChan & Wolfram Alpha Non-Commercial API", 8, yPosition + 20, titlePaint);

            var data = surface
                .Snapshot()
                .Encode(SKEncodedImageFormat.Jpeg, 90)
                .ToArray();

            return data;
        });
    }
}