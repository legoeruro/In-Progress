using UnityEngine;
using TMPro;
using System;

[CreateAssetMenu(menuName = "In Progress/Forms/Form Text Formatting Config")]
public class FormTextFormattingConfig : ScriptableObject
{
    [Header("Format Blocks")]
    public TextFormatConfig title = TextFormatConfig.CreateDefaultTitle();
    public TextFormatConfig h1 = TextFormatConfig.CreateDefaultH1();
    public TextFormatConfig h2 = TextFormatConfig.CreateDefaultH2();
    public TextFormatConfig h3 = TextFormatConfig.CreateDefaultH3();
    public TextFormatConfig body = TextFormatConfig.CreateDefaultBody();
}

[Serializable]
public class TextFormatConfig
{
    [Header("Typography")]
    public float fontSize = 3f;
    public float lineHeight = 14f;

    [Header("Color")]
    public Color color = Color.black;

    [Header("Alignment")]
    public TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft;

    // TODO: thing
    [Header("Style")]
    public TMPro.FontStyles fontStyle = FontStyles.Normal;

    public static TextFormatConfig CreateDefaultTitle()
    {
        return new TextFormatConfig
        {
            fontSize = 6f,
            lineHeight = 25f,
            color = Color.black,
            alignment = TextAlignmentOptions.TopLeft,
            fontStyle = FontStyles.Bold
        };
    }

    public static TextFormatConfig CreateDefaultH1()
    {
        return new TextFormatConfig
        {
            fontSize = 5f,
            lineHeight = 20f,
            color = Color.black,
            alignment = TextAlignmentOptions.TopLeft,
            fontStyle = FontStyles.Bold
        };
    }

    public static TextFormatConfig CreateDefaultH2()
    {
        return new TextFormatConfig
        {
            fontSize = 4f,
            lineHeight = 18f,
            color = Color.black,
            alignment = TextAlignmentOptions.TopLeft,
            fontStyle = FontStyles.Bold
        };
    }

    public static TextFormatConfig CreateDefaultH3()
    {
        return new TextFormatConfig
        {
            fontSize = 3.5f,
            lineHeight = 16f,
            color = new Color(0.2f, 0.2f, 0.2f),
            alignment = TextAlignmentOptions.TopLeft,
            fontStyle = FontStyles.Bold
        };
    }

    public static TextFormatConfig CreateDefaultBody()
    {
        return new TextFormatConfig
        {
            fontSize = 3f,
            lineHeight = 14f,
            color = Color.black,
            alignment = TextAlignmentOptions.TopLeft,
            fontStyle = FontStyles.Normal
        };
    }
}