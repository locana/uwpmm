using Kazyx.RemoteApi.AvContent;
using Kazyx.Uwpmm.CameraControl;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kazyx.Uwpmm.Playback
{
    public class RemoteApiContentsLoader : ContentsLoader
    {
        private readonly AvContentApiClient AvContentApi;

        private readonly string Udn;

        public const int CONTENT_LOOP_STEP = 50;

        public RemoteApiContentsLoader(TargetDevice device)
        {
            AvContentApi = device.Api.AvContent;
            Udn = device.Udn;
        }

        public override async Task Load(CancellationTokenSource cancel)
        {
            if (!await IsStorageSupportedAsync().ConfigureAwait(false))
            {
                DebugUtil.Log("Storage scheme is not available on this device");
                throw new StorageNotSupportedException();
            }

            var storages = await GetStoragesUriAsync().ConfigureAwait(false);
            if (cancel != null && cancel.IsCancellationRequested)
            {
                DebugUtil.Log("Loading task cancelled");
                OnCancelled();
                return;
            }

            if (storages.Count == 0)
            {
                DebugUtil.Log("No storage is available on this device");
                throw new NoStorageException();
            }

            await GetContentsByDateSeparatelyAsync(storages[0], cancel).ConfigureAwait(false);
        }

        /// <summary>
        /// Camera devices should support "storage" scheme.
        /// </summary>
        /// <param name="av"></param>
        /// <returns></returns>
        private async Task<bool> IsStorageSupportedAsync()
        {
            var schemes = await AvContentApi.GetSchemeListAsync();
            foreach (var scheme in schemes)
            {
                if (scheme.Scheme == Scheme.Storage)
                {
                    DebugUtil.Log("Storage scheme is supported");
                    return true;
                }
            }
            DebugUtil.Log("Storage scheme is NOT supported");
            return false;
        }

        private async Task<IList<string>> GetStoragesUriAsync()
        {
            var sources = await AvContentApi.GetSourceListAsync(new UriScheme { Scheme = Scheme.Storage }).ConfigureAwait(false);
            var list = new List<string>(sources.Count);
            foreach (var source in sources)
            {
                list.Add(source.Source);
            }
            return list;
        }

        private async Task GetContentsByDateSeparatelyAsync(string uri, CancellationTokenSource cancel)
        {
            DebugUtil.Log("Loading number of Dates");

            var count = await AvContentApi.GetContentCountAsync(new CountingTarget
            {
                Grouping = ContentGroupingMode.Date,
                Uri = uri,
            }).ConfigureAwait(false);

            DebugUtil.Log(count.NumOfContents + " dates exist.");

            if (cancel != null && cancel.IsCancellationRequested)
            {
                DebugUtil.Log("Loading task cancelled");
                OnCancelled();
                return;
            }

            var loops = count.NumOfContents / CONTENT_LOOP_STEP + (count.NumOfContents % CONTENT_LOOP_STEP == 0 ? 0 : 1);

            for (var i = 0; i < loops; i++)
            {
                var dates = await GetDateListAsync(uri, i * CONTENT_LOOP_STEP, CONTENT_LOOP_STEP).ConfigureAwait(false);
                if (cancel != null && cancel.IsCancellationRequested)
                {
                    DebugUtil.Log("Loading task cancelled");
                    OnCancelled();
                    break;
                }

                foreach (var date in dates)
                {
                    await GetContentsOfDaySeparatelyAsync(date, true, cancel).ConfigureAwait(false);
                }
            }
        }

        private async Task<IList<DateInfo>> GetDateListAsync(string uri, int startFrom, int count)
        {
            DebugUtil.Log("Loading DateList: " + uri + " from " + startFrom);

            var contents = await AvContentApi.GetContentListAsync(new ContentListTarget
            {
                Sorting = SortMode.Descending,
                Grouping = ContentGroupingMode.Date,
                Uri = uri,
                StartIndex = startFrom,
                MaxContents = count
            }).ConfigureAwait(false);

            var list = new List<DateInfo>();
            foreach (var content in contents)
            {
                if (content.IsFolder == TextBoolean.True)
                {
                    list.Add(new DateInfo { Title = content.Title, Uri = content.Uri });
                }
            }
            return list;
        }

        private async Task GetContentsOfDaySeparatelyAsync(DateInfo date, bool includeMovies, CancellationTokenSource cancel)
        {
            DebugUtil.Log("Loading: " + date.Title);

            var count = await AvContentApi.GetContentCountAsync(new CountingTarget
            {
                Grouping = ContentGroupingMode.Date,
                Uri = date.Uri,
            }).ConfigureAwait(false);

            DebugUtil.Log(count.NumOfContents + " contents exist.");

            var loops = count.NumOfContents / CONTENT_LOOP_STEP + (count.NumOfContents % CONTENT_LOOP_STEP == 0 ? 0 : 1);

            for (var i = 0; i < loops; i++)
            {
                var contents = await GetContentsOfDayAsync(date, i * CONTENT_LOOP_STEP, CONTENT_LOOP_STEP, includeMovies).ConfigureAwait(false);
                if (cancel != null && cancel.IsCancellationRequested)
                {
                    DebugUtil.Log("Loading task cancelled");
                    OnCancelled();
                    break;
                }

                DebugUtil.Log(contents.Count + " contents fetched");
                var list = new List<Thumbnail>();
                foreach (var content in contents)
                {
                    list.Add(new Thumbnail(content, Udn));
                }

                OnPartLoaded(list);
            }
        }

        private async Task<IList<ContentInfo>> GetContentsOfDayAsync(DateInfo date, int startFrom, int count, bool includeMovies)
        {
            DebugUtil.Log("Loading ContentsOfDay: " + date.Title + " from " + startFrom);

            var types = new List<string>();
            types.Add(ContentKind.StillImage);
            if (includeMovies)
            {
                types.Add(ContentKind.MovieMp4);
                types.Add(ContentKind.MovieXavcS);
            }

            var contents = await AvContentApi.GetContentListAsync(new ContentListTarget
            {
                Sorting = SortMode.Ascending,
                Grouping = ContentGroupingMode.Date,
                Uri = date.Uri,
                Types = types,
                StartIndex = startFrom,
                MaxContents = count
            }).ConfigureAwait(false);

            var list = new List<ContentInfo>();
            foreach (var content in contents)
            {
                if (content.ImageContent != null
                    && content.ImageContent.OriginalContents != null
                    && content.ImageContent.OriginalContents.Count > 0)
                {
                    var contentInfo = new RemoteApiContentInfo
                    {
                        Name = RemoveExtension(content.ImageContent.OriginalContents[0].FileName),
                        LargeUrl = content.ImageContent.LargeImageUrl,
                        ThumbnailUrl = content.ImageContent.ThumbnailUrl,
                        ContentType = content.ContentKind,
                        Uri = content.Uri,
                        CreatedTime = content.CreatedTime,
                        Protected = content.IsProtected == TextBoolean.True,
                        RemotePlaybackAvailable = (content.RemotePlayTypes != null && content.RemotePlayTypes.Contains(RemotePlayMode.SimpleStreaming)),
                        GroupName = date.Title,
                    };

                    if (content.ContentKind == ContentKind.StillImage)
                    {
                        foreach (var original in content.ImageContent.OriginalContents)
                        {
                            if (original.Type == ImageType.Jpeg)
                            {
                                contentInfo.OriginalUrl = original.Url;
                                break;
                            }
                        }
                    }

                    list.Add(contentInfo);
                }
            }

            return list;
        }

        private static string RemoveExtension(string name)
        {
            if (name == null)
            {
                return "";
            }
            if (!name.Contains("."))
            {
                return name;
            }
            else
            {
                var index = name.LastIndexOf(".");
                if (index == 0)
                {
                    return "";
                }
                else
                {
                    return name.Substring(0, index);
                }
            }
        }
    }

    public class NoStorageException : Exception
    {
    }

    public class StorageNotSupportedException : Exception
    {
    }

    public class DateInfo
    {
        public string Title { set; get; }
        public string Uri { set; get; }
    }
}
