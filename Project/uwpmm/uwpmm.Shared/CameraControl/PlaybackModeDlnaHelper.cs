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
    }
}
