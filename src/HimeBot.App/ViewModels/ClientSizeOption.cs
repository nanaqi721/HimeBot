namespace HimeBot.App.ViewModels;

public sealed record ClientSizeOption(
    int Width,
    int Height)
{
    public string DisplayName =>
        $"{Width} × {Height}";
}