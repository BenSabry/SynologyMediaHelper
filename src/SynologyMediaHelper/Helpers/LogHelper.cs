namespace SynologyMediaHelper.Helpers;
public class LogHelper
{
    #region Fields-Static
    private static readonly string TempDirectory = @$"{CommonHelper.BaseDirectory}\Temp\Logs";
    public static DirectoryInfo LogDirectory => new DirectoryInfo(TempDirectory);
    #endregion

    #region Fields-Instance
    private readonly string CurrentPath;
    private readonly bool Enabled;
    #endregion

    #region Constructors
    public LogHelper(bool enabled = true)
    {
        Enabled = enabled;

        if (Enabled)
            CurrentPath = GenerateNewFile();
    }
    #endregion

    #region Behavior-Static
    private static string GenerateNewFile()
    {
        if (!Directory.Exists(TempDirectory))
            Directory.CreateDirectory(TempDirectory);

        var date = DateTime.Now;
        const int length = 2;

        var path = @$"{TempDirectory}\{date.Year}"
            + $"{CommonHelper.FormatNumberToLength(date.Month, length)}"
            + $"{CommonHelper.FormatNumberToLength(date.Day, length)}"
            + $"{CommonHelper.FormatNumberToLength(date.Hour, length)}"
            + $"{CommonHelper.FormatNumberToLength(date.Minute, length)}"
            + $".{Guid.NewGuid().ToString().Replace("-", string.Empty)}"
            + ".txt";

        if (File.Exists(path))
            File.Delete(path);

        File.CreateText(path).Close();

        return path;
    }

    public static void Log(string message)
    {
        Console.WriteLine($"{message}");
    }
    public static void Clear()
    {
        Console.Clear();
    }
    #endregion

    #region Behavior-Instance
    public void LogToDisk(string message)
    {
        if (Enabled)
            File.AppendAllLines(CurrentPath, new[] { message });
    }
    #endregion
}
