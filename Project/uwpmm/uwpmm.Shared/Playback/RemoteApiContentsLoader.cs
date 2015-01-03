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

        public override async Task Start(CancellationTokenSource cancel)
        {
            if (!await IsStorageSupportedAsync().ConfigureAwait(false))
            {
                DebugUtil.Log("Storage scheme is not available on this device");
                throw new StorageNotSupportedException();
            }

            var storages = await GetStoragesUriAsync().ConfigureAwait(false);
            if (cancel != null && cancel.IsCancellationRequested)
            {
                OnCancelled();
                return;
            }

            if (storages.Count == 0)
            {
                DebugUtil.Log("No storage is available on this device");
                throw new NoStorageException();
            }

            await GetDateListAsEventsAsync(storages[0], async (dates) =>
            {
                foreach (var date in dates)
                {
                    await GetContentsOfDayAsEventsAsync(date, true, (e2) =>
                    {
                        var list = new List<Thumbnail>();
                        foreach (var content in e2)
                        {
                            list.Add(new Thumbnail(content, Udn));
                        }
                        OnPartLoaded(list);
                    }, cancel).ConfigureAwait(false);
                }
            }, cancel).ConfigureAwait(false);
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
                    return true;
                }
            }
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

        private async Task GetDateListAsEventsAsync(string uri, Action<IList<DateInfo>> handler, CancellationTokenSource cancel)
        {
            var count = await AvContentApi.GetContentCountAsync(new CountingTarget
            {
                Grouping = ContentGroupingMode.Date,
                Uri = uri,
            }).ConfigureAwait(false);

            if (cancel != null && cancel.IsCancellationRequested)
            {
                OnCancelled();
                return;
            }

            var loops = count.NumOfContents / CONTENT_LOOP_STEP + (count.NumOfContents % CONTENT_LOOP_STEP == 0 ? 0 : 1);

            for (var i = 0; i < loops; i++)
            {
                var dates = await GetDateListAsync(uri, i * CONTENT_LOOP_STEP, CONTENT_LOOP_STEP).ConfigureAwait(false);
                if (cancel != null && cancel.IsCancellationRequested)
                {
                    OnCancelled();
                    break;
                }
                handler.Invoke(dates);
            }
        }

        private async Task<IList<DateInfo>> GetDateListAsync(string uri, int startFrom, int count)
        {
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

        private async Task GetContentsOfDayAsEventsAsync(DateInfo date, bool includeMovies, Action<IList<ContentInfo>> handler, CancellationTokenSource cancel)
        {
            var count = await AvContentApi.GetContentCountAsync(new CountingTarget
            {
                Grouping = ContentGroupingMode.Date,
                Uri = date.Uri,
            }).ConfigureAwait(false);

            var loops = count.NumOfContents / CONTENT_LOOP_STEP + (count.NumOfContents % CONTENT_LOOP_STEP == 0 ? 0 : 1);

            for (var i = 0; i < loops; i++)
            {
                var contents = await GetContentsOfDayAsync(date, i * CONTENT_LOOP_STEP, CONTENT_LOOP_STEP, includeMovies).ConfigureAwait(false);
                if (cancel != null && cancel.IsCancellationRequested)
                {
                    OnCancelled();
                    break;
                }
                handler.Invoke(contents);
            }
        }

        private async Task<IList<ContentInfo>> GetContentsOfDayAsync(DateInfo date, int startFrom, int count, bool includeMovies)
        {
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
                    var contentInfo = new WebApiContentInfo
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
}
