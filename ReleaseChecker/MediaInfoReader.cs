using MediaInfoLib;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ReleaseChecker
{
    public static class MediaInfoReader
    {
        internal static string SafeGet(MediaInfo mi, StreamKind kind, int streamNumber, string parameter)
        {
            try
            {
                var v = mi.Get(kind, streamNumber, parameter);
                return string.IsNullOrEmpty(v) ? string.Empty : v;
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static long SafeGetLong(MediaInfo mi, StreamKind kind, int streamNumber, string parameter)
        {
            try
            {
                var v = mi.Get(kind, streamNumber, parameter);
                if (string.IsNullOrWhiteSpace(v)) return 0;

                if (long.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var l)) return l;

                var m = Regex.Match(v, "[0-9]+(?:\\.[0-9]+)?");
                if (m.Success)
                {
                    if (long.TryParse(m.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var l2)) return l2;
                    if (double.TryParse(m.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return (long)d;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        internal static bool? SafeGetTag(MediaInfo mi, StreamKind kind, int streamNumber, string parameter)
        {
            try
            {
                var value = mi.Get(kind, streamNumber, parameter);

                if (string.IsNullOrWhiteSpace(value)) return false;

                var v = value.Trim().ToLowerInvariant();
                if (v == "yes" || v == "y" || v == "1" || v == "true" || v == "��") return true;

                return false;
            }
            catch
            {
                return null;
            }
        }

        internal static bool IsZeroOrEmpty(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;

            var trimmed = value.Trim();
            if (trimmed == "0") return true;

            if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return Math.Abs(d) < 0.000001;

            return false;
        }
    }
}