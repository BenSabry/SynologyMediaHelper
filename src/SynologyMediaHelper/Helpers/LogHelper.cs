namespace SynologyMediaHelper.Helpers;
public class LogHelper
{
    #region Fields-Static
    private static readonly string TempDirectory;
    #endregion

    #region Fields-Instance
    private readonly string CurrentPath;
    #endregion

    #region Constructors
    static LogHelper()
    {
        TempDirectory = @$"{CommonHelper.BaseDirectory}\Temp\Logs";
        if (!Directory.Exists(TempDirectory))
            Directory.CreateDirectory(TempDirectory);
    }
    public LogHelper()
    {
        CurrentPath = GenerateNewFile();
    }
    #endregion

    #region Behavior-Static
    private static string GenerateNewFile()
    {
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
        Console.WriteLine($"{DateTime.Now} {message}");
    }
    public static void Clear()
    {
        Console.Clear();
    }
    public static DirectoryInfo GetLogDirectory()
    {
        return new DirectoryInfo(TempDirectory);
    }
    #endregion

    #region Behavior-Instance
    public void LogToDisk(string message)
    {
        File.AppendAllLines(CurrentPath, new[] { message });
    }
    #endregion
}
