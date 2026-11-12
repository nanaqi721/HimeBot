using System.Drawing;

namespace HimeBot.Platform.Windows.ScreenCapture;

internal static class ClientCropRectangleCalculator
{
    internal static bool TryCalculate(
        Size frameSize,
        Size clientSize,
        Point clientOrigin,
        Point windowOrigin,
        out Rectangle cropRectangle,
        out string errorMessage)
    {
        cropRectangle = Rectangle.Empty;
        errorMessage = string.Empty;

        if(clientSize.Width <=0 || clientSize.Height <=0) 
        {
            errorMessage = "游戏客户区尺寸无效。";
            return false;
        }
        int cropX = clientOrigin.X - windowOrigin.X;
        int cropY = clientOrigin.Y - windowOrigin.Y;

        if(cropX < 0 ||
            cropY < 0 ||
            cropX + clientSize.Width > frameSize.Width ||
            cropY + clientSize.Height > frameSize.Height)
        {
            errorMessage =
                $"客户区裁剪范围超出 WGC 帧：" +
                $"帧为 {frameSize.Width} × {frameSize.Height}，" +
                $"裁剪区域为 ({cropX}, {cropY}, " +
                $"{clientSize.Width}, {clientSize.Height})。";
            return false;

        }
        cropRectangle = new Rectangle(
            cropX,
            cropY,
            clientSize.Width,
            clientSize.Height);

        return true;
    }
}