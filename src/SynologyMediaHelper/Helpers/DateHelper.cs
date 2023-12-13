using System.Globalization;
using System.Text.RegularExpressions;

namespace SynologyMediaHelper.Helpers;
public static class DateHelper
{
    #region Fields-Static
    private const int RegexTimeoutMilliseconds = 100;
    private static readonly Func<string, (bool Valid, DateTime Value)>[] Parsers = [MatchFormatedRegex, MatchNumericRegex];

    private static readonly Regex[] NumericsRegexes = [CreateRegex(@"\d+")];
    private static readonly Regex[] InvalidRegexes = [CreateRegex(@"(^FB_IMG_)(\d+)"), CreateRegex(@"(^received_)(\d+)")];
    private static readonly (Regex, string)[] FormatedRegexes = new string[]
    {
        "yyyyMMdd_HHmmss",
        "yyyyMMdd-HHmmss",
        "yyyyMMdd HHmmss",

        "yyyy_MM_dd_HH_mm_ss",
        "yyyy_MM_dd-HH_mm_ss",
        "yyyy_MM_dd HH_mm_ss",
        "yyyy-MM-dd_HH-mm-ss",
        "yyyy-MM-dd-HH-mm-ss",
        "yyyy-MM-dd HH-mm-ss",
        "yyyy MM dd_HH mm ss",
        "yyyy MM dd-HH mm ss",
        "yyyy MM dd HH mm ss",

        "dd MMM YYYY HH:mm:ss",
        "dd MMM YYYY HH_mm_ss",
        "dd MMM YYYY HH-mm-ss",

        "yyyyMMdd",
        "yyyy-MM-dd",
        "yyyy_MM_dd"
    }
        .Select(format => (CreateRegex(GenerateRegexPatternByDateFormat(format)), format))
        .ToArray();
    #endregion

    #region Behavior
    public static bool TryExtractMinimumValidDateTime(string s, out DateTime result)
    {
        if (MatchesInvalidRegex(s))
        {
            result = default;
            return false;
        }

        var dates = new List<DateTime>();
        foreach (var parser in Parsers)
        {
            var res = parser(s);
            if (res.Item1 && IsValidDateTime(res.Item2))
                dates.Add(res.Item2);
        }

        if (dates.Count > 0)
        {
            result = dates.Min();
            return true;
        }

        result = default;
        return false;
    }

    public static bool TryParseDateTime(string s, string format, out DateTime result)
    {
        return DateTime.TryParseExact(s, format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out result);
    }
    public static bool IsValidDateTime(DateTime date)
    {
        return date < DateTime.Now && date.Year > 2000;
    }
    private static bool IsValidDateTime(long number)
    {

        try
        {
            if (number >= DateTime.MinValue.Ticks && number <= DateTime.MaxValue.Ticks)
            {
                var date = new DateTime(number);
                if (IsValidDateTime(date))
                    return true;
            }
        }
        catch { }

        try
        {
            var date = DateTime.FromBinary(number);
            if (IsValidDateTime(date))
                return true;
        }
        catch { }

        try
        {
            var date = DateTime.FromFileTimeUtc(number);
            if (IsValidDateTime(date))
                return true;
        }
        catch { }

        return false;
    }

    private static Regex CreateRegex(string s)
    {
        return new Regex(s, RegexOptions.Compiled,
            new TimeSpan(0, 0, 0, 0, RegexTimeoutMilliseconds));
    }
    public static bool MatchesInvalidRegex(string s)
    {
        return TryMatchesRegex(s, InvalidRegexes, out var _);
    }
    private static (bool, DateTime) MatchFormatedRegex(string s)
    {
        foreach (var i in FormatedRegexes)
        {
            var match = i.Item1.Match(s);
            if (match.Success && TryParseDateTime(match.Value, i.Item2, out var date))
                return (true, date);
        }

        return (false, default);
    }
    private static (bool, DateTime) MatchNumericRegex(string s)
    {
        while (TryMatchesRegex(s, NumericsRegexes, out Match match))
        {
            if (long.TryParse(match.Value, out var res) && IsValidDateTime(res))
                return (true, new DateTime(res));

            s = s.Replace(match.Value, string.Empty, StringComparison.Ordinal);
        }

        return (false, default);
    }
    private static bool TryMatchesRegex(string s, Regex[] regexes, out Match result)
    {
        foreach (var regex in regexes)
        {
            result = regex.Match(s);
            if (result.Success)
                return true;
        }

        result = Match.Empty;
        return false;
    }
    private static string GenerateRegexPatternByDateFormat(string format)
    {
        const string millisecond = @"(\\d{3})";
        const string minute = @"(0[0-9]|[1-5][0-9])";
        const string hour = @"(0[0-9]|1[0-9]|2[0-3])";
        const string dayNumber = @"(0[1-9]|[12][0-9]|3[01])";
        const string dayName = @"((?:Mon|Tue(?:s)?|Wed(?:nes)?|Thu(?:rs)?|Fri|Sat(?:ur)?|Sun)(?:day)?)";
        const string monthNumber = @"(?:0[1-9]|1[0-2])";
        const string monthName = @"(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)";
        const string year = @"((19|20)\d\d)";

        var pairs = new (string Match, string Pattern, bool CaseSensitive)[]
        {
            ("YYYY", year, false),
            ("MMM", monthName, false),
            ("MM", monthNumber, true),
            ("DDD", dayName, false),
            ("DD", dayNumber, false),
            ("HH", hour, false),
            ("mm", minute, true),
            ("ss", minute, false),
            ("fff", millisecond, false),
        };

        var regex = new string(format);
        foreach (var item in pairs)
            regex = regex.Replace(item.Match, item.Pattern, item.CaseSensitive
                ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

        return regex;
    }
    #endregion
}
