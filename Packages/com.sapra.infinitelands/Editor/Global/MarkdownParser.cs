using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using UnityEngine;

namespace sapra.InfiniteLands.Editor{
public static class MarkdownParser
{
    public static void ParseMarkdownToVisualElements(string markdown, VisualElement rootContainer)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            rootContainer.Add(new Label("Changelog is empty."));
            return;
        }

        rootContainer.Clear();
        string[] lines = markdown.Split('\n');

        bool first = true;
        foreach (string line in lines)
        {
            string trimmedLine = line.TrimStart();
            if (string.IsNullOrEmpty(trimmedLine)) continue;

            VisualElement lineContainer = new VisualElement 
            { 
                style = { 
                    flexDirection = FlexDirection.Row,
                    marginLeft = CountIndentLevel(line) * 20
                }
            };

            if (trimmedLine.StartsWith("# "))
            {
                if(!first)
                    lineContainer.style.marginTop = 20;
                lineContainer.style.borderTopWidth = 1;
                lineContainer.style.borderTopColor = new Color(0.482f, 0.482f, 0.482f);
                lineContainer.style.paddingTop = 4;

                first = false;
                AddFormattedLabel(lineContainer, trimmedLine.Substring(2).Trim(), 24, true);
            }
            else if (trimmedLine.StartsWith("## "))
            {
                lineContainer.style.marginTop = 15;
                AddFormattedLabel(lineContainer, trimmedLine.Substring(3).Trim(), 18, true);
            }
            else if (trimmedLine.StartsWith("- "))
            {
                AddFormattedLabel(lineContainer, "• " + trimmedLine.Substring(2), 12, false);
            }
            else
            {
                AddFormattedLabel(lineContainer, trimmedLine, 12, false);
            }

            rootContainer.Add(lineContainer);
        }
    }

    private static void AddFormattedLabel(VisualElement container, string text, int fontSize, bool isBold)
    {
        // First handle bold
        string formattedText = Regex.Replace(text, @"\*\*(.*?)\*\*", "<b>$1</b>");
        // Then handle italics with color
        formattedText = Regex.Replace(formattedText, @"\*(.*?)\*", "<color=#A0A0A0><i>$1</i></color>");
            
            string urlPattern = @"(https?:\/\/[^\s]+)";
            string[] segments = Regex.Split(formattedText, urlPattern);

            foreach (string segment in segments)
            {
                if (string.IsNullOrEmpty(segment)) continue;

                Label label = new Label
                {
                    text = segment,
                    style = {
                        whiteSpace = WhiteSpace.Normal,
                        fontSize = fontSize,
                        unityFontStyleAndWeight = isBold ? FontStyle.Bold : FontStyle.Normal
                    }
                };

                if (Regex.IsMatch(segment, urlPattern))
                {
                    label.style.color = new Color(0, 0.7f, 1f); // Blue for URLs
                    label.RegisterCallback<ClickEvent>(evt => Application.OpenURL(segment));
                    label.style.cursor = new StyleCursor(StyleKeyword.Initial);
                }

                container.Add(label);
            }
        }

        private static int CountIndentLevel(string line)
        {
            int indent = 0;
            for (int i = 0; i < line.Length && char.IsWhiteSpace(line[i]); i++)
            {
                if (line[i] == '\t') indent++;
                else indent += line[i] == ' ' ? 1 / 4 : 0; // Count 4 spaces as one indent
            }
            return indent;
        }
    }
}