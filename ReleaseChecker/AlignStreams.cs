namespace ReleaseChecker
{
    public static class AlignStreams
    {
        public static void Start(List<MediaFileInfo> files)
        {
            if (files == null || files.Count < 2)
                return;

            Align<AudioStreamInfo>(files, GetFileWithMaxAmountOfStreams<AudioStreamInfo>(files));

            Align<SubtitleStreamInfo>(files, GetFileWithMaxAmountOfStreams<SubtitleStreamInfo>(files));
        }

        private static MediaFileInfo? GetFileWithMaxAmountOfStreams<T>(List<MediaFileInfo> files) where T : CoreStreamInfo
        {
            MediaFileInfo? result = null;
            int maxCount = 0;

            foreach (var file in files)
            {
                var streams = file.GetStreamList<T>();
                int count = streams.Count;

                if (result == null || count > maxCount)
                {
                    result = file;
                    maxCount = count;
                }

            }

            return result;
        }

        private static void Align<T>(List<MediaFileInfo> files, MediaFileInfo? max_streams_file) where T : CoreStreamInfo
        {
            if (max_streams_file == null)
                return;

            var titles_ref = max_streams_file
                .GetStreamList<T>()
                .OfType<CoreStreamInfo>()
                .Select(x => x.Title)
                .ToList();

            foreach (var file in files)
                AlignFileStreams<T>(file, titles_ref);
        }

        private static void AlignFileStreams<T>(MediaFileInfo file, List<string> titles_ref) where T : CoreStreamInfo
        {
            var file_streams = file.GetStreamList<T>();

            if (file_streams.Count == titles_ref.Count)
                return;

            var streams = new List<T?>(file_streams);

            var titles_in_file = streams
                .OfType<CoreStreamInfo>()
                .Select(x => x.Title)
                .ToList();

            foreach (var title in titles_in_file)
                if (!titles_ref.Contains(title))
                    return;

            for (var i = streams.Count; i < titles_ref.Count; i++)
            {
                file_streams.Add(null);
            }

            for (var i = titles_ref.Count - 1; i >= 0; i--)
            {
                var stream = streams.FindAll(x => x?.Title == titles_ref[i]);

                file_streams[i] = stream.FirstOrDefault();
            }

            var streamsWithoutNulls = file_streams
                .OfType<CoreStreamInfo>()
                .Where(x => x != null)
                .ToList();

            for (int i = 0; i < streamsWithoutNulls.Count - 2; i++)
            {
                for (int j = i + 1; j < streamsWithoutNulls.Count - 1; j++)
                {
                    if (streamsWithoutNulls[i].Index > streamsWithoutNulls[j].Index)
                    {
                        file.SetStreamList<T>(streams);
                        return;
                    }
                }
            }
        }
    }
}
