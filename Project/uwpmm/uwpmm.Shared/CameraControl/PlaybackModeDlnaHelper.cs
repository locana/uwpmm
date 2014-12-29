using Kazyx.Uwpmm.UPnP;
using Kazyx.Uwpmm.UPnP.ContentDirectory;
using Kazyx.Uwpmm.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kazyx.Uwpmm.CameraControl
{
    public class PlaybackModeDlnaHelper
    {
        public PlaybackModeDlnaHelper(UpnpDevice upnp)
        {
            UpnpDevice = upnp;
        }

        private readonly UpnpDevice UpnpDevice;

        public event Action<IList<Item>> ItemsRetrieved;

        public event Action FetchingCompleted;

        public event Action<Result> BrowsingInProgress;

        public event Action BrowsingCompleted;

        public async Task RetrieveAllImageMetadataAsync(CancellationTokenSource cancel = null)
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
                RequestedCount = PlaybackModeHelper.CONTENT_LOOP_STEP,
            });
            if (cancel != null && cancel.IsCancellationRequested)
            {
                return;
            }

            var contents = res as RetrievedContents;
            ItemsRetrieved.Raise(contents.Result.Items);

            if (contents.TotalMatches > (start + 1) * PlaybackModeHelper.CONTENT_LOOP_STEP)
            {
                await RetrieveAllImageMetadataRecursivelyAsync(cancel, start + 1);
            }
            else
            {
                FetchingCompleted.Raise();
            }
        }

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

        public static Resource GetThumbnailResource(IList<Resource> resources)
        {
            if (resources.Count == 0)
            {
                return null;
            }

            Resource result = null;
            foreach (var res in resources)
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
    }
}
