﻿using System.Text;

namespace SynologyMediaHelper.Helpers;
public static class CommonHelper
{
    #region Properties
    public static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;
    public static string ToolsDirectory => Path.Combine(BaseDirectory, "Tools");
    
    private static readonly char[] separator = ['\r', '\n'];
    #endregion

    #region Behavior
    public static string FormatNumberToLength(int number, int length)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < length - number.ToString().Length; i++)
            builder.Append('0');

        return builder.Append(number).ToString();
    }
    public static string[] SplitStringLines(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? [] : text.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim()).ToArray();
    }
    #endregion
}
