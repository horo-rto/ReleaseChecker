using MediaInfoLib;

namespace ReleaseChecker
{
    public class AudioStreamInfo : CoreStreamInfo
    {
        public string FormatProfile { get; set; }
        public string AdditionalFeatures { get; set; }
        public string NumberOfDynamicObjects { get; set; }
        public string Channels { get; set; }
        public string ChannelPositions { get; set; }
        public string SamplingRate { get; set; }
        public string BitDepth { get; set; }
        public string Duration { get; set; }

        public AudioStreamInfo(MediaInfo mi, int i, MediaFileInfo parent) : base(mi, i, StreamKind.Audio, parent)
        {
            FormatProfile = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "Format_Profile");
            AdditionalFeatures = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "Format_AdditionalFeatures");
            NumberOfDynamicObjects = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "NumberOfDynamicObjects");
            Channels = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "Channel(s)");
            ChannelPositions = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "ChannelLayout");
            SamplingRate = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "SamplingRate/String");
            BitDepth = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "BitDepth");
            Duration = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "Duration/String3");
        }

        public string ChannelsToString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Channels)) return string.Empty;

                var s = Channels.ToLowerInvariant();

                if (s.Contains("stereo")) return "2.0";
                if (s.Contains("mono")) return "1.0";

                if (string.IsNullOrWhiteSpace(ChannelPositions)) return Channels + ".0";

                var cp = ChannelPositions.ToLowerInvariant();
                bool hasLfe = cp.Contains("lfe") || cp.Contains("low frequency");

                if (int.TryParse(Channels, out var n))
                {
                    if (hasLfe) return (n - 1) + ".1";
                    return n + ".0";
                }

                return Channels;
            }
        }

        public string FormatRewrittenWithLayer => Format switch
        {
            "MPEG Audio" => "MP" + FormatProfile.Replace("Layer ", ""),
            "AAC" => "AAC" + (string.IsNullOrEmpty(FormatProfile) ? "" : " " + FormatProfile),
            "MLP FBA" => "Dolby TrueHD",
            "DTS XLL" => "DHS-HD MA",
            _ => Format,
        };

        public string FormatToString => AdditionalFeatures switch
        {
            "" => FormatRewrittenWithLayer,
            "XLL" => "DHS-HD MA",
            "JOC" => FormatRewrittenWithLayer + " Atmos",
            _ => NumberOfDynamicObjects switch
            {
                "" => FormatRewrittenWithLayer + " " + AdditionalFeatures,
                _ => FormatRewrittenWithLayer + " Atmos (" + NumberOfDynamicObjects + " objects)",
            },
        };

        public bool PercentageError => ParentFile.VideoStream?.StreamSizeBytes > 0 ? (StreamSizeBytes * 100f / ParentFile.VideoStream.StreamSizeBytes) > 33.333 : false;
        public bool ChannelsError { get; set; }
        public bool LanguageOrderError { get; set; }

        public new string ToString
        {
            get
            {
                return $"{DefaultToString}{ForcedToString}{Percentage} {Language} {Title}" +
                    $"\n{Format}; {ChannelsToString}; {SamplingRate}; {BitRateToString}";
            }
        }
    }
}