using SynologyMediaHelper.Helpers;

namespace SynologyMediaHelper.Core;
public static class Engine
{
    #region Fields-Static
    private static readonly Settings Settings = SettingsHelper.GetSettings();
    private static DateTime StartDateTime;

    private static int Moves;
    private static int Updates;
    private static int Fails;
    #endregion

    #region Behavior
    private static void InitializeTimer()
    {
        StartDateTime = DateTime.Now;
    }
    private static void ValidateTasksConfig()
    {
        if (Settings.TasksCount < 0 || Environment.ProcessorCount == 1)
            Settings.TasksCount = 1;

        else if (Settings.TasksCount > 1 && Environment.ProcessorCount == 2)
            Settings.TasksCount = 2;

        else if (Settings.TasksCount > Environment.ProcessorCount / 2)
            Settings.TasksCount = Environment.ProcessorCount / 2;
    }

    public static void Run()
    {
        if (!SourcesAreValidToUse()) return;

        InitializeTimer();
        ValidateTasksConfig();

        GetMediaFiles().ForEachUsingTasks(
            Settings.TasksCount,
            (FileInfo file, long i, (ExifHelper exif, LogHelper logger) arg)
                => Process(file, i, arg.exif, arg.logger),

            () => (new ExifHelper(), new LogHelper(Settings.EnableLog)),
            LogProgress);

        Task.Delay(1000).Wait();
        LogHelper.Log("\nCleaning Up...");

        if (Settings.ClearBackupFilesOnComplete) ExifHelper.ClearBackupFiles(Settings.Sources);
        if (Settings.DeleteEmptyDirectoriesOnComplete) DeleteEmptyDirectories();
    }
    private static void Process(FileInfo file, long index, ExifHelper exif, LogHelper logger)
    {
        if (!file.Exists) return;

        var date = new DateTime[][]
        {
            DateHelper.ExtractAllPossibleDateTimes(file.Name),
            exif.ReadAllDates(file.FullName),

            [ exif.ReadMediaDefaultCreationDate(file.FullName) ]
        }
            .SelectMany()
            .Where(DateHelper.IsValidDateTime)
            .MinOrDefault();

        UpdateMediaTargetedDateTime(file, index, date, exif, logger);
        MoveFileDirectoryBasedOnDate(ref file, index, date, logger);
    }

    private static bool SourcesAreValidToUse()
    {
        if (Settings.Sources.All(string.IsNullOrWhiteSpace))
        {
            LogHelper.Log("PleaseAddSourcesToAppSettings");
            return false;
        }
        return true;
    }
    private static IEnumerable<FileInfo> GetMediaFiles()
    {
        foreach (var source in Settings.Sources)
            if (!string.IsNullOrWhiteSpace(source))
            {
                if (File.Exists(source))
                {
                    var file = new FileInfo(source);
                    if (IsSupportedMediaFile(file))
                        yield return file;
                }

                else if (Directory.Exists(source))
                {
                    var sources = Settings.Sources
                        .Where(i => !i.Equals(source, StringComparison.Ordinal))
                        .ToArray();

                    foreach (var file in GetMediaFiles(new DirectoryInfo(source), sources))
                        yield return file;
                }
            }
    }
    private static IEnumerable<FileInfo> GetMediaFiles(DirectoryInfo directory, string[] sources)
    {
        if (!sources.Contains(directory.FullName))
            foreach (var file in directory.EnumerateFiles().Where(IsSupportedMediaFile))
                yield return file;

        foreach (var dir in directory.EnumerateDirectories())
            foreach (var file in GetMediaFiles(dir, sources))
                yield return file;
    }
    private static void DeleteEmptyDirectories()
    {
        foreach (var source in Settings.Sources)
            if (string.IsNullOrWhiteSpace(source) && Directory.Exists(source))
                DeleteEmptyDirectories(new DirectoryInfo(source));
    }
    private static void DeleteEmptyDirectories(DirectoryInfo directory)
    {
        foreach (var dir in directory.EnumerateDirectories())
            DeleteEmptyDirectories(dir);

        if (!directory.EnumerateFiles().Any()
            && !directory.EnumerateDirectories().Any())
            directory.Delete();
    }
    private static bool IsSupportedMediaFile(FileInfo file)
    {
        return !string.IsNullOrWhiteSpace(file.Name)
            && ExifHelper.IsSupportedMediaFile(file)
            && SynologyHelper.IsSupportedMediaFile(file);
    }

    private static void MoveFileDirectoryBasedOnDate(ref FileInfo file, long index, DateTime dateTime, LogHelper logger)
    {
        var dest = GetNewDestinationPath(file, dateTime);

        if (!Equals(file.FullName, dest))
            TryMoveFile(logger, ref file, index, dest);
    }
    private static void MakeSureDirectoryExistsForFile(string filePath)
    {
        var dir = new FileInfo(filePath).Directory;

        if (!dir!.Exists)
            dir.Create();
    }
    private static void TryMoveFile(LogHelper logger, ref FileInfo file, long index, string dest)
    {
        if (File.Exists(dest))
        {
            LogFail(logger, index, file.FullName);
            return;
        }

        try
        {
            MakeSureDirectoryExistsForFile(dest);
            var src = file.FullName;
            file.MoveTo(dest);
            LogMove(logger, index, src, dest);
        }
        catch
        {
            LogFail(logger, index, dest);
        }
    }

    private static void UpdateMediaTargetedDateTime(FileInfo file, long index, DateTime dateTime, ExifHelper exif, LogHelper logger)
    {
        var valid = exif.TryUpdateMediaTargetedDateTime(file.FullName, dateTime);

        if (valid) LogUpdate(logger, index, file.FullName);
        else LogFail(logger, index, file.FullName);
    }
    private static string GetNewDestinationPath(FileInfo file, DateTime dateTime)
        => Path.Combine(Settings.Target,
            CommonHelper.FormatNumberToLength(dateTime.Year, 4),
            CommonHelper.FormatNumberToLength(dateTime.Month, 2),
            file.Name);

    private static void LogMove(LogHelper logger, long index, string src, string dest)
    {
        Interlocked.Add(ref Moves, 1);
        logger.LogToDisk($"{index}\tMove\t{src}\t{dest}");
    }
    private static void LogUpdate(LogHelper logger, long index, string src)
    {
        Interlocked.Add(ref Updates, 1);
        logger.LogToDisk($"{index}\tUpdate\t{src}");
    }
    private static void LogFail(LogHelper logger, long index, string src)
    {
        Interlocked.Add(ref Fails, 1);
        logger.LogToDisk($"{index}\tFail\t{src}");
    }
    private static void LogProgress(long index, long total)
    {
        var t = DateTime.Now - StartDateTime;
        var r = (t / (index + 1)) * (total - index);
        var processes = index / t.TotalSeconds;

        LogHelper.Clear();
        LogHelper.Log(
            $"SynologyMediaHelper by BenSabry\n"
            + $"https://github.com/BenSabry/SynologyMediaHelper\n\n"
            + $"Elapsed time: {t.Hours}:{t.Minutes}:{t.Seconds}\n"
            + $"Elapsed time: {r.Hours}:{r.Minutes}:{r.Seconds}\n"
            + $"Parallel Tasks Running: {Settings.TasksCount}\n\n"

            + $"Currnet: {index}/{total} Moved:{Moves} Updated: {Updates}\n"
            + $"Processing Speed: {(int)processes}/Second {(int)(processes * 60)}/Minute {(int)(processes * 3600)}/Hour\n"
            + GenerateConsoleProgressBar(index, total));
    }

    public static string GenerateConsoleProgressBar(long index, long total)
    {
        return GenerateProgressBar(index, total, Console.WindowWidth);
    }
    private static string GenerateProgressBar(long index, long total, long width)
    {
        if (total == default)
        {
            index++;
            total++;
        }

        var perc = Math.Round((index / (decimal)total) * 100, 2);
        var percText = $"[ {perc}% ";

        if (width < percText.Length + 2)
            return $"{percText}]";

        var done = Math.Max(((width - percText.Length) / (decimal)100 * perc) - 2, default);
        var remain = (width - (percText.Length)) - (int)done;

        return $"{percText}{new string('-', (int)done)}{new string(' ', (int)remain - 1)}]";
    }
    #endregion

    #region TEST
    public static IEnumerable<FileInfo> InitAndGetFilesForTest()
    {
        Settings.Target = "C:\\Data\\SynologyMediaHelperTEST\\Target";
        Settings.Sources = [
            "C:\\Data\\SynologyMediaHelperTEST\\Sources\\"
        ];

        if (!SourcesAreValidToUse()) return [];

        InitializeTimer();
        ValidateTasksConfig();

        return GetMediaFiles();
    }
    #endregion
}