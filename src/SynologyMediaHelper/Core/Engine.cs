using SynologyMediaHelper.Helpers;

namespace SynologyMediaHelper.Core;
public static class Engine
{
    #region Fields-Static
    private static readonly Settings Settings = SettingsHelper.GetSettings();
    private static readonly List<FileInfo> Files = new List<FileInfo>();
    private static int TaskDelayTimeForEnumerationTask = Settings.TasksCount * 10;
    private static bool FilesEnumerationTaskFinished;

    private static DateTime StartDateTime;
    private static int StartFileIndex;
    private static int CurrentFileIndex;

    private static int Moves;
    private static int Updates;
    private static int Fails;
    #endregion

    #region Constructors

    #endregion

    #region Behavior
    private static void InitializeResume()
    {
        LogHelper.Log("InitializeStartPoint");

        var logs = LogHelper.GetLogDirectory()
            .GetFiles().OrderByDescending(i => i.LastWriteTime)
            .ToArray();

        foreach (var log in logs)
        {
            var lines = File.ReadAllLines(log.FullName);
            if (lines.Length == default)
                continue;

            var parts = lines[lines.Length - 1].Split('\t');
            if (parts.Length > 0 && int.TryParse(parts[0], out int index))
            {
                StartFileIndex = index;
                return;
            }
        }
    }
    private static void InitializeTimer()
    {
        StartDateTime = DateTime.Now;
    }

    public static void Run()
    {
        if (Settings.EnableResume)
            InitializeResume();

        InitializeTimer();

        var tasks = new List<Task>
        {
            RunMediaFilesEnumerationTask()
        };

        for (int i = 0; i < Settings.TasksCount; i++)
        {
            var id = i;
            tasks.Add(Task.Run(() =>
            {
                RunTask(id);
            }));
        }

        Task.WaitAll(tasks.ToArray());
        LogProgress(string.Empty);

        if (Settings.ClearBackupFilesOnComplete)
            LogHelper.Log(ExifHelper.ClearBackupFiles(Settings.Sources));

        if (Settings.DeleteEmptyDirectoriesOnComplete)
        {
            DeleteEmptyDirectories();
            LogHelper.Log("DeleteEmptyDirectoriesOnComplete");
        }
    }
    private static void RunTask(int index)
    {
        LogHelper.Log($"Task {index + 1} Initialized");

        var exif = new ExifHelper();
        var logger = new LogHelper();

        while (index < Files.Count || !FilesEnumerationTaskFinished)
        {
            Thread.Sleep(TaskDelayTimeForEnumerationTask);
            for (int i = index; i < Files.Count; i = index += Settings.TasksCount)
            {
                Interlocked.Add(ref CurrentFileIndex, 1);
                if (StartFileIndex > i) continue;

                var file = Files[i];
                if (!file.Exists)
                    continue;

                //to prevent too much console messages
                if ((i % (Settings.TasksCount + 1)) == 0)
                    LogProgress(file.FullName);


                if (exif.TryReadMediaDefaultCreationDate(file.FullName, out var date))
                    MoveFileDirectoryBasedOnDate(logger, ref file, date);

                else if (DateHelper.TryExtractMinimumValidDateTime(file.Name, out date))
                {
                    if (exif.TryReadMinimumDate(file.FullName, out var infoDate)
                        && (infoDate.Date == date.Date || infoDate < date))
                        date = infoDate;

                    UpdateMediaTargetedDateTime(exif, logger, file, date);
                    MoveFileDirectoryBasedOnDate(logger, ref file, date);
                }
            }
        }

        LogResult(logger);
    }

    private static Task RunMediaFilesEnumerationTask()
    {
        LogHelper.Log("InizializeMediaFiles");

        return Task.Run(() =>
        {
            foreach (var source in Settings.Sources)
                if (!string.IsNullOrWhiteSpace(source))
                {
                    if (File.Exists(source))
                        Files.Add(new FileInfo(source));
                    else if (Directory.Exists(source))
                        EnumerateMediaFiles(new DirectoryInfo(source));
                }

            FilesEnumerationTaskFinished = true;
        });
    }
    private static void EnumerateMediaFiles(DirectoryInfo directory)
    {
        foreach (var file in directory.GetFiles().Where(IsSupportedMediaFile))
            if (!Files.Exists(i => i.FullName.Equals(file.FullName, StringComparison.Ordinal)))
                Files.Add(file);

        foreach (var dir in directory.GetDirectories())
            EnumerateMediaFiles(dir);
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

    private static void MoveFileDirectoryBasedOnDate(LogHelper logger, ref FileInfo file, DateTime dateTime)
    {
        if (FilePathIsAlignedWithDate(file, dateTime) || !file.Exists) return;

        var dest = @$"{file.Directory!.Parent!.Parent}\{dateTime.Year}\"
            + @$"{CommonHelper.FormatNumberToLength(dateTime.Month, 2)}\{file.Name}";

        TryMoveFile(logger, ref file, dest);
    }
    private static void MakeSureDirectoryExistsForFile(string filePath)
    {
        var dir = new FileInfo(filePath).Directory;

        if (!dir!.Exists)
            dir.Create();
    }
    private static void TryMoveFile(LogHelper logger, ref FileInfo src, string dest)
    {
        if (File.Exists(dest))
        {
            LogFail(logger, src.FullName);
            return;
        }

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            MakeSureDirectoryExistsForFile(dest);
            src.MoveTo(dest);
            LogMove(logger, src.FullName, dest);
        }
        catch
        {
            LogFail(logger, dest);
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    private static void UpdateMediaTargetedDateTime(ExifHelper exif, LogHelper logger, FileInfo file, DateTime dateTime)
    {
        var valid = exif.TryUpdateMediaTargetedDateTime(file.FullName, dateTime);

        if (valid) LogUpdate(logger, file.FullName);
        else LogFail(logger, file.FullName);
    }
    private static bool FilePathIsAlignedWithDate(FileInfo file, DateTime dateTime)
    {
#pragma warning disable CA1305 // Specify IFormatProvider
        return dateTime.Year == int.Parse(file.Directory!.Parent!.Name)
            && dateTime.Month == int.Parse(file.Directory!.Name);
#pragma warning restore CA1305 // Specify IFormatProvider
    }

    private static void LogMove(LogHelper logger, string src, string dest)
    {
        Interlocked.Add(ref Moves, 1);
        logger.LogToDisk($"{CurrentFileIndex}\tMove\t{src}\t{dest}");
    }
    private static void LogUpdate(LogHelper logger, string src)
    {
        Interlocked.Add(ref Updates, 1);
        logger.LogToDisk($"{CurrentFileIndex}\tUpdate\t{src}");
    }
    private static void LogFail(LogHelper logger, string src)
    {
        Interlocked.Add(ref Fails, 1);
        logger.LogToDisk($"{CurrentFileIndex}\tFail\t{src}");
    }

    private static void LogProgress(string message)
    {
#pragma warning disable S6561 // Avoid using "DateTime.Now" for benchmarking or timing operations
        var t = DateTime.Now - StartDateTime;
#pragma warning restore S6561 // Avoid using "DateTime.Now" for benchmarking or timing operations
        var actions = (Moves + Updates) / t.TotalSeconds;
        var processes = CurrentFileIndex / t.TotalSeconds;

        LogHelper.Clear();
        LogHelper.Log(
            "\n\n"
            + $"{t.Hours}H:{t.Minutes}M:{t.Seconds}S Currnet: {CurrentFileIndex}/{Files.Count} Moved:{Moves} Updated: {Updates}\n"
            + $"Processing Speed: {(int)processes}/Second {(int)(processes * 60)}/Minute {(int)(processes * 3600)}/Hour\n"
            + $"Actions Speed: {(int)actions}/Second {(int)(actions * 60)}/Minute {(int)(actions * 3600)}/Hour\n"
            + $"{message}\n\n\n");
    }
    private static void LogResult(LogHelper logger)
    {
        logger.LogToDisk($"{CurrentFileIndex}  Moved: {Moves}  Updated:  {Updates}");
    }
    #endregion
}
