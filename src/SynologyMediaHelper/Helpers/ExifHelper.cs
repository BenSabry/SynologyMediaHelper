using System.Text;

namespace SynologyMediaHelper.Helpers;
public class ExifHelper
{
    #region Fields-Static
    private const string ExifTool = "exiftool.exe";
    private const string ExifDateFormat = "yyyy:MM:dd HH:mm:sszzz";
    private const string ExifDefaultCreationTag = "FileCreateDate";
    private const bool AttemptToFixMediaIncorrectOffsets = true;

    private static readonly char[] separator = new[] { '\t' };
    private static readonly string ToolsDirectory = CommonHelper.ToolsDirectory;
    private static readonly string TempDirectory = Path.Combine(CommonHelper.BaseDirectory, @"Temp\Tools");

    private static readonly string[] ExifTargetedDateTimeTags = new[] { ExifDefaultCreationTag, "FileModifyDate" };
    private static readonly string[] SupportedMediaExtensions;
    #endregion

    #region Fields-Instance
    private readonly string Id;
    #endregion

    #region Constructors
    static ExifHelper()
    {
        if (!File.Exists(Path.Combine(ToolsDirectory, ExifTool)))
            throw new FileNotFoundException("ExifTool is missing!");

        if (Directory.Exists(TempDirectory))
            Directory.Delete(TempDirectory, true);

        Directory.CreateDirectory(TempDirectory);

        SupportedMediaExtensions =
            ExifExecute(ToolsDirectory, ExifTool, "-T -listf")
            .ToLower()
            .Replace("\r\n", " ")
            .Split(' ')
            .Select(i => $".{i}")
            .ToArray();
    }
    public ExifHelper()
    {
        Id = $"{Guid.NewGuid().ToString().Replace("-", string.Empty)}.exe";

        File.Copy(Path.Combine(ToolsDirectory, ExifTool),
            Path.Combine(TempDirectory, Id));
    }
    #endregion

    #region Destructor
    ~ExifHelper()
    {
        File.Delete(Path.Combine(TempDirectory, Id));
    }
    #endregion

    #region Behavior-Static
    private static string ExifExecute(string dir, string id, string arg)
    {
        var output = ProcessHelper.RunAndGetOutput(id, dir, arg);

        if (string.IsNullOrWhiteSpace(output))
            return string.Empty;

        return output.Trim();
    }
    private static string ExifRead(string id, string args, string path)
    {
        return ExifExecute(TempDirectory, id, string.Concat(args, $" \"{path}\""));
    }
    private static bool TryExifWrite(string id, string args, string path)
    {
        if (AttemptToFixMediaIncorrectOffsets) args += " -F";

        var output = ExifRead(id, args, path);
        var lines = CommonHelper.SplitStringLines(output);

        var updates = 0;
        var errors = 0;

        const string updateMessage = "image files updated";
        const string errorMessage = "files weren't updated due to errors";
        const char Splitter = ' ';

        foreach (var line in lines)
        {
            var parts = line.Split(Splitter);
            if (!int.TryParse(parts[0], out int value))
                continue;

            var message = line.Remove(0, parts[0].Length + 1);
            switch (message)
            {
                case updateMessage: updates = value; break;
                case errorMessage: errors = value; break;
                default: break;
            }
        }

        return updates > 0 && errors == 0;
    }

    private static string DateTimeFormat(DateTime dateTime)
    {
        return dateTime.ToString(ExifDateFormat);
    }
    public static bool IsSupportedMediaFile(FileInfo file)
    {
        return SupportedMediaExtensions.Contains(file.Extension.ToLower());
    }
    public static string ClearBackupFiles(params string[] sources)
    {
        if (sources is null)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var src in sources)
            if (string.IsNullOrWhiteSpace(src) && Directory.Exists(src))
                builder.AppendLine(ClearBackupFiles(new DirectoryInfo(src)));

        var lines = CommonHelper.SplitStringLines(builder.ToString());

        var directories = 0;
        var images = 0;
        var originals = 0;

        const string directoryMessage = "directories scanned";
        const string imagesMessage = "image files found";
        const string originalsMessage = "original files deleted";
        const char Splitter = ' ';

        foreach (var line in lines)
        {
            var parts = line.Split(Splitter);
            if (!int.TryParse(parts[0], out int value))
                continue;

            var msg = line.Remove(0, parts[0].Length + 1);
            switch (msg)
            {
                case directoryMessage: directories += value; break;
                case imagesMessage: images += value; break;
                case originalsMessage: originals += value; break;
                default: break;
            }
        }

        return $"\n{directories} {directoryMessage}\n{images} {imagesMessage}\n{originals} backup files deleted";
    }
    private static string ClearBackupFiles(DirectoryInfo directory)
    {
        var builder = new StringBuilder();
        foreach (var dir in directory.GetDirectories())
            builder.AppendLine(ClearBackupFiles(dir));

        return builder.AppendLine(ExifExecute(ToolsDirectory, ExifTool,
            $"-overwrite_original -delete_original! \"{directory.FullName}\""))
            .ToString();
    }
    public static string ReadVersion()
    {
        return ExifExecute(ToolsDirectory, ExifTool, "-ver");
    }
    #endregion

    #region Behavior-Instance
    public bool TryReadMinimumDate(string path, out DateTime result)
    {
        result = ReadMinimumDate(path);
        return result != default;
    }
    public DateTime ReadMinimumDate(string path)
        => ReadAllDates(path).Where(DateHelper.IsValidDateTime).MinOrDefault();
    public DateTime[] ReadAllDates(string path)
    {
        return ExifRead(Id, "-T -AllDates", path)
            .Split(separator, StringSplitOptions.RemoveEmptyEntries)
            .Select(i =>
            {
                DateHelper.TryParseDateTime(i.Trim(), ExifDateFormat, out DateTime dateTime);
                return dateTime;
            })
            .Where(DateHelper.IsValidDateTime)
            .Distinct()
            .ToArray();
    }

    public bool TryReadMediaDefaultCreationDate(string path, out DateTime dateTime)
    {
        dateTime = ReadMediaDefaultCreationDate(path);
        return DateHelper.IsValidDateTime(dateTime);
    }
    public DateTime ReadMediaDefaultCreationDate(string path)
    {
        DateHelper.TryParseDateTime(
            ExifRead(Id, $"-T -{ExifDefaultCreationTag}", path),
            ExifDateFormat, out var dateTime);

        return dateTime;
    }
    public bool TryUpdateMediaTargetedDateTime(string path, DateTime dateTime)
    {
        var formatedDateTime = DateTimeFormat(dateTime);

        const char sperator = ' ';
        var args = new string(
            string.Join(sperator,
                ExifTargetedDateTimeTags.Select(i => $"\"-{i}={formatedDateTime}\""))
                .ToArray());

        return TryExifWrite(Id, args, path);
    }
    #endregion
}
