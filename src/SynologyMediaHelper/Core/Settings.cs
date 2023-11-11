using Microsoft.Extensions.Configuration;

public sealed class Settings
{
    #region Fields
    private readonly IConfiguration Configuration;
    #endregion

    #region Properties
    public int TasksCount { get; set; }
    public bool EnableResume { get; set; }
    public bool EnableLog { get; set; }
    public bool AttemptToFixMediaIncorrectOffsets { get; set; }
    public bool ClearBackupFilesOnComplete { get; set; }
    public bool DeleteEmptyDirectoriesOnComplete { get; set; }
    public string[] Sources { get; set; }
    #endregion

    #region Constructors
    public Settings(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    #endregion
}

