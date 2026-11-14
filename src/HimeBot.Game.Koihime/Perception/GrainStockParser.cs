using HimeBot.Game.Koihime.Domain;
using System.Globalization;
using System.Text;

namespace HimeBot.Game.Koihime.Perception;

/// <summary>
/// 将粮食库存 OCR 文字规范化并解析为游戏状态。
/// </summary>
public sealed class GrainStockParser
{
    private const char ValueSeparator = '/';
    private const char GrainUnit = '万';

    public GrainStockParseResult Parse(string? rawText)
    {
        string normalizedText = Normalize(rawText);

        if (normalizedText.Length == 0)
        {
            return CreateFailure(
                "OCR 原文清理空白后为空。",
                normalizedText);
        }

        string[] parts =
            normalizedText.Split(ValueSeparator);

        if (parts.Length != 2 ||
            parts[0].Length == 0 ||
            parts[1].Length == 0)
        {
            return CreateFailure(
                "粮食文字必须符合“当前值/容量”格式。",
                normalizedText);
        }

        if (!TryParseAmount(
                parts[0],
                "当前粮食",
                out int currentAmount,
                out string currentAmountError))
        {
            return CreateFailure(
                currentAmountError,
                normalizedText);
        }

        if (!TryParseAmount(
                parts[1],
                "粮食容量",
                out int capacity,
                out string capacityError))
        {
            return CreateFailure(
                capacityError,
                normalizedText);
        }


        return new GrainStockParseResult(
            true,
            "粮食文字解析成功。",
            normalizedText,
            new GrainStock(
                currentAmount,
                capacity));
    }

    private static string Normalize(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        string compatibilityNormalized =
            rawText.Normalize(NormalizationForm.FormKC);

        var builder =
            new StringBuilder(
                compatibilityNormalized.Length);

        foreach (char character in compatibilityNormalized)
        {
            if (!char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static bool TryParseAmount(
        string text,
        string fieldName,
        out int value,
        out string errorMessage)
    {
        value = 0;

        if (!text.EndsWith(GrainUnit))
        {
            errorMessage =
                $"{fieldName}必须以“万”为单位。";

            return false;
        }

        string digits = text[..^1];

        if (!int.TryParse(
                digits,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out value))
        {
            errorMessage =
                $"{fieldName}的“万”前面必须是非负整数。";

            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static GrainStockParseResult CreateFailure(
        string message,
        string normalizedText)
    {
        return new GrainStockParseResult(
            false,
            message,
            normalizedText,
            null);
    }
}