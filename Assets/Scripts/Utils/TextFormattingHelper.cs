using TMPro;
using UnityEngine;

public static class TextFormattingHelper
{
    public static void ApplyStyles(TextMeshProUGUI textComponent, FormContentType contentType, FormTextFormattingConfig config)
    {
        if (textComponent == null) return;

        TextFormatConfig format = GetFormat(contentType, config);
        textComponent.fontSize = format.fontSize;
        textComponent.color = format.color;
        textComponent.alignment = format.alignment;
        textComponent.fontStyle = format.fontStyle;
    }

    public static float GetLineHeight(FormContentType contentType, FormTextFormattingConfig config)
    {
        TextFormatConfig format = GetFormat(contentType, config);
        return format.lineHeight;
    }

    private static TextFormatConfig GetFormat(FormContentType contentType, FormTextFormattingConfig config)
    {
        if (config == null)
        {
            return contentType switch
            {
                FormContentType.Title => TextFormatConfig.CreateDefaultTitle(),
                FormContentType.H1 => TextFormatConfig.CreateDefaultH1(),
                FormContentType.H2 => TextFormatConfig.CreateDefaultH2(),
                FormContentType.H3 => TextFormatConfig.CreateDefaultH3(),
                FormContentType.Text => TextFormatConfig.CreateDefaultBody(),
                _ => TextFormatConfig.CreateDefaultBody(),
            };
        }

        return contentType switch
        {
            FormContentType.Title => config.title ?? TextFormatConfig.CreateDefaultTitle(),
            FormContentType.H1 => config.h1 ?? TextFormatConfig.CreateDefaultH1(),
            FormContentType.H2 => config.h2 ?? TextFormatConfig.CreateDefaultH2(),
            FormContentType.H3 => config.h3 ?? TextFormatConfig.CreateDefaultH3(),
            FormContentType.Text => config.body ?? TextFormatConfig.CreateDefaultBody(),
            _ => config.body ?? TextFormatConfig.CreateDefaultBody(),
        };
    }
}