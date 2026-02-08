using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReleaseChecker
{
    public static class Checker
    {
        public static void CheckConsistency(List<MediaFileInfo> files)
        {
            if (files == null || files.Count < 2) return;

            // Video consistency
            var videos = files.Where(f => f.VideoStream != null).Select(f => f.VideoStream!).ToList();

            if (videos.Count >= 2)
            {
                MarkOutliers(videos, a => a.Title, (a, e) => a.TitleError = e);
                MarkOutliers(videos, v => v.Format, (v, e) => v.FormatError = e);
                MarkOutliers(videos, v => v.BitDepth, (v, e) => v.BitDepthError = e);
                MarkOutliers(videos, v => v.FrameRateToString, (v, e) => v.FrameRateError = e);
                MarkOutliers(videos, v => $"{v.Width}x{v.Height}", (v, e) => v.ResolutionError = e);
                MarkBitrateOutliers(videos);
            }

            // Audio consistency per stream index
            int maxAudio = files.Max(f => f.AudioStreams?.Count ?? 0);
            for (int i = 0; i < maxAudio; i++)
            {
                int idx = i;
                var audios = files
                    .Where(f => f.AudioStreams != null && idx < f.AudioStreams.Count)
                    .Select(f => f.AudioStreams[idx])
                    .ToList();

                if (audios.Count >= 2)
                {
                    MarkOutliers(audios, a => a.Title, (a, e) => a.TitleError = e);
                    MarkOutliers(audios, a => a.Language, (a, e) => a.LanguageError = e);
                    MarkOutliers(audios, a => a.Format, (a, e) => a.FormatError = e);
                    MarkOutliers(audios, a => a.ChannelsToString, (a, e) => a.ChannelsError = e);
                    MarkBitrateOutliers(audios);
                }
            }

            // Subtitle consistency per stream index
            int maxSubs = files.Max(f => f.SubtitleStreams?.Count ?? 0);
            for (int i = 0; i < maxSubs; i++)
            {
                int idx = i;
                var subs = files
                    .Where(f => f.SubtitleStreams != null && idx < f.SubtitleStreams.Count)
                    .Select(f => f.SubtitleStreams[idx])
                    .ToList();

                if (subs.Count >= 2)
                {
                    MarkOutliers(subs, s => s.Language, (s, e) => s.LanguageError = e);
                    MarkOutliers(subs, s => s.Format, (s, e) => s.FormatError = e);
                }
            }
        }

        private static void MarkOutliers<T>(List<T> items, Func<T, string> getValue, Action<T, bool> setError)
        {
            var mode = items
                .GroupBy(getValue)
                .OrderByDescending(g => g.Count())
                .First().Key;

            foreach (var item in items)
                setError(item, getValue(item) != mode);
        }

        private static void MarkBitrateOutliers<T>(List<T> streams) where T : CoreStreamInfo
        {
            var parsed = streams
                .Select(a => (Stream: a, Value: ParseBitrate(a.BitRate)))
                .Where(p => p.Value > 0)
                .ToList();

            if (parsed.Count < 2)
            {
                foreach (var s in streams) s.BitRateError = 0;
                return;
            }

            var sorted = parsed.OrderBy(p => p.Value).ToList();
            double median = sorted[sorted.Count / 2].Value;

            foreach (var p in parsed)
            {
                if (Math.Abs(p.Value - median) / median > 0.5)
                    p.Stream.BitRateError = 5;
                else if (Math.Abs(p.Value - median) / median > 0.4)
                    p.Stream.BitRateError = 4;
                else if(Math.Abs(p.Value - median) / median > 0.3)
                    p.Stream.BitRateError = 3;
                else if (Math.Abs(p.Value - median) / median > 0.2)
                    p.Stream.BitRateError = 2;
                else if (Math.Abs(p.Value - median) / median > 0.1)
                    p.Stream.BitRateError = 1;
                else
                    p.Stream.BitRateError = 0;
            }    
        }

        private static double ParseBitrate(string br)
        {
            if (string.IsNullOrWhiteSpace(br)) return 0;

            // Collapse spaces within numbers: "1 536" → "1536"
            var s = Regex.Replace(br.Trim(), @"(\d)\s+(\d)", "$1$2");
            var m = Regex.Match(s, @"([\d.]+)\s*(k|M|G)?b", RegexOptions.IgnoreCase);

            if (!m.Success) return 0;
            if (!double.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                return 0;

            var unit = m.Groups[2].Value;
            if (unit.Equals("k", StringComparison.OrdinalIgnoreCase)) return val * 1000;
            if (unit.Equals("M", StringComparison.Ordinal)) return val * 1_000_000;
            if (unit.Equals("G", StringComparison.OrdinalIgnoreCase)) return val * 1_000_000_000;
            return val;
        }
    }
}
