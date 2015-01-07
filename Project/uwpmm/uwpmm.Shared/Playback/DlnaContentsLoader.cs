using Kazyx.RemoteApi.AvContent;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.UPnP;
using Kazyx.Uwpmm.UPnP.ContentDirectory;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kazyx.Uwpmm.Playback
{
    public class DlnaContentsLoader : ContentsLoader
    {
        private readonly UpnpDevice UpnpDevice;

        public const int CONTENT_LOOP_STEP = 50;

        public DlnaContentsLoader(UpnpDevice upnp)
        {
            UpnpDevice = upnp;
        }

        public override async Task Load(CancellationTokenSource cancel)
        {
            await RetrieveAllImageMetadataRecursivelyAsync(cancel, 0);
        }

        private async Task RetrieveAllImageMetadataRecursivelyAsync(CancellationTokenSource cancel, int start)
        {
            var res = await UpnpDevice.Services[URN.ContentDirectory].Control(new SearchRequest
            {
                ContainerID = "0",
                SearchCriteria = "upnp:class derivedfrom \"" + Class.ImageItem + "\"",
                StartingIndex = start,
                RequestedCount = CONTENT_LOOP_STEP,
            });
            if (cancel != null && cancel.IsCancellationRequested)
            {
                OnCancelled();
                return;
            }

            var contents = res as RetrievedContents;
            OnPartLoaded(Translate(contents.Result.Items));

            if (contents.TotalMatches > (start + 1) * CONTENT_LOOP_STEP)
            {
                await RetrieveAllImageMetadataRecursivelyAsync(cancel, start + 1);
            }
            else
            {
                OnCompleted();
            }
        }

        private IList<Thumbnail> Translate(IList<Item> source)
        {
            return source.Where(item => item.Resources.Count != null)
                .Select(item => new Thumbnail(Translate(item), UpnpDevice.UDN))
                .ToList();
        }

        private static DlnaContentInfo Translate(Item source)
        {
            if (source.Resources.Count == 0)
            {
                return null;
            }

            return new DlnaContentInfo
            {
                Id = source.Id,
                ContentType = source.Class == Class.ImageItem ? ContentKind.StillImage : ContentKind.Unknown,
                CreatedTime = source.Date,
                Name = source.Title,
                Protected = source.Restricted,
                OriginalUrl = GetOriginalImageResource(source).ResourceUrl,
                LargeUrl = GetLargeImageResource(source).ResourceUrl,
                ThumbnailUrl = GetThumbnailResource(source).ResourceUrl,
                GroupName = source.Genre,
            };
        }

        private static Resource GetLargeImageResource(Item item)
        {
            var matched = item.Resources
                .FirstOrDefault(res => res.ProtocolInfo != null && res.ProtocolInfo.MimeType == "image/jpeg" && res.ProtocolInfo.DlnaProfileName == DlnaProfileName.JpegLarge);

            if (matched != null)
            {
                return matched;
            }

            if (item.Class == Class.ImageItem)
            {
                return item.Resources[0];
            }
            return null;
        }

        private static Resource GetOriginalImageResource(Item item)
        {
            var matched = item.Resources
                .FirstOrDefault(res => res.ProtocolInfo != null && res.ProtocolInfo.MimeType == "image/jpeg" && res.ProtocolInfo.IsOriginalContent);

            if (matched != null)
            {
                return matched;
            }

            if (item.Class == Class.ImageItem)
            {
                return item.Resources[0];
            }
            return null;
        }

        private static Resource GetThumbnailResource(Item item)
        {
            Resource result = null;
            foreach (var res in item.Resources)
            {
                if (res.ProtocolInfo == null)
                {
                    continue;
                }
                if (res.ProtocolInfo.MimeType != "image/jpeg")
                {
                    continue;
                }

                if (res.ProtocolInfo.DlnaProfileName == DlnaProfileName.JpegSmall)
                {
                    // Small is the best.
                    result = res;
                    break;
                }
                if (res.ProtocolInfo.DlnaProfileName == DlnaProfileName.JpegThumbnail)
                {
                    // Thumbnail is the second best.
                    result = res;
                    continue;
                }
                if (res.ProtocolInfo.DlnaProfileName == DlnaProfileName.JpegMedium)
                {
                    if (result == null || result.ProtocolInfo.DlnaProfileName == DlnaProfileName.JpegLarge)
                    {
                        // Medium is better than Large.
                        result = res;
                        continue;
                    }
                }
                if (result == null)
                {
                    result = res;
                    continue;
                }
            }

            return result;
        }

        /*
        public event Action<Result> BrowsingInProgress;

        public event Action BrowsingCompleted;

        public async Task<Result> BrowseChildGradually(string containerId, CancellationTokenSource cancel = null)
        {
            return await BrowseChild(containerId, cancel, 0);
        }

        private async Task<Result> BrowseChild(string containerId, CancellationTokenSource cancel, int start)
        {
            var res = await UpnpDevice.Services[URN.ContentDirectory].Control(new BrowseRequest
            {
                ObjectID = containerId,
                BrowseFlag = BrowseFlag.BrowseDirectChildren,
                StartingIndex = start,
                RequestedCount = PlaybackModeHelper.CONTENT_LOOP_STEP,
            });
            if (cancel != null && cancel.IsCancellationRequested)
            {
                throw new OperationCanceledException("Browse child is cancelled.");
            }

            var contents = res as RetrievedContents;
            if (contents.TotalMatches > (start + 1) * PlaybackModeHelper.CONTENT_LOOP_STEP)
            {
                BrowsingInProgress.Raise(contents.Result);
                return await BrowseChild(containerId, cancel, start + 1);
            }
            else
            {
                BrowsingCompleted.Raise();
                return contents.Result;
            }
        }
         * */
    }
}
