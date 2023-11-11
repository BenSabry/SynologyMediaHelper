public class SynologyHelper
{
    #region Fields-Static
    private static readonly string[] SupportedPhotoExtensions;
    private static readonly string[] SupportedVideoExtensions;
    #endregion

    #region Constructors
    static SynologyHelper()
    {
        SupportedVideoExtensions = new[] { "3G2", "3GP", "ASF", "AVI", "DAT", "DivX", "FLV", "M4V", "MOV", "MP4", "MPEG", "MPG", "MTS", "M2TS", "M2T", "QT", "WMV", "XviD" }.Select(i => $".{i.ToLower()}").ToArray();
        SupportedPhotoExtensions = new[] { "BMP", "JPG", "JPEG", "GIF", "RAW", "TIFF", "PNG" }.Select(i => $".{i.ToLower()}").ToArray();
    }
    #endregion

    #region Behavior-Static
    public static bool IsSupportedPhotoFile(FileInfo file)
    {
        return SupportedPhotoExtensions.Contains(file.Extension.ToLower());
    }
    public static bool IsSupportedVideoFile(FileInfo file)
    {
        return SupportedVideoExtensions.Contains(file.Extension.ToLower());
    }
    public static bool IsSupportedMediaFile(FileInfo file)
    {
        return IsSupportedPhotoFile(file) || IsSupportedVideoFile(file);
    }
    #endregion
}