using System.Text;

public static class CommonHelper
{
    #region Behavior
    public static string GetBaseDirectory()
    {
        return AppDomain.CurrentDomain.BaseDirectory;
    }
    public static string GetMediaDirectory()
    {
        return @"\\SabryDS\home\Photos";
    }

    public static string FormatNumberToLength(int number, int length)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < length - number.ToString().Length; i++)
            builder.Append('0');

        return builder.Append(number).ToString();
    }
    public static string[] SplitStringLines(string text)
    {
        return text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(i => i.Trim()).ToArray();
    }
    #endregion
}
