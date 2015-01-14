using Kazyx.RemoteApi.AvContent;
using Kazyx.Uwpmm.DataModel;
using Kazyx.Uwpmm.UPnP;
using Kazyx.Uwpmm.UPnP.ContentDirectory;
using Kazyx.Uwpmm.Utility;
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

        public override async Task Load(ContentsSet contentsSet, CancellationTokenSource cancel)
        {
            await RetrieveContentsByBrowse("Root", "0", cancel);
            // await RetrieveAllImageMetadataRecursivelyAsync(cancel, 0);
            OnCompleted();
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
            // TODO
            OnPartLoaded(Translate("Image files", contents.Result.Items));

            if (contents.TotalMatches > (start + 1) * CONTENT_LOOP_STEP)
            {
                await RetrieveAllImageMetadataRecursivelyAsync(cancel, start + 1);
            }
            else
            {
                OnCompleted();
            }
        }

        private IList<Thumbnail> Translate(string containerName, IList<Item> source)
        {
            var group = FormatDateTitle(containerName);
            return source.Where(item => item.Resources.Count != 0)
                .Select(item => new Thumbnail(Translate(group, item), UpnpDevice.UDN))
                .ToList();
        }

        private static DlnaContentInfo Translate(string containerName, Item source)
        {
            if (source.Resources.Count == 0)
            {
                return null;
            }

            var type = ParseContentType(source.Class);

            return new DlnaContentInfo
            {
                Id = source.Id,
                ContentType = type,
                CreatedTime = source.Date,
                Name = source.Title,
                Protected = source.Restricted,
                OriginalUrl = GetOriginalImageResource(source),
                LargeUrl = GetLargeImageResource(source),
                ThumbnailUrl = GetThumbnailResource(source),
                GroupName = containerName,
                RemotePlaybackAvailable = type == ContentKind.StillImage,
            };
        }

        private static string ParseContentType(string upnpClass)
        {
            if (upnpClass.StartsWith(Class.ImageItem))
            {
                return ContentKind.StillImage;
            }
            if (upnpClass.StartsWith(Class.VideoItem))
            {
                // TODO other types
                return ContentKind.MovieMp4;
            }
            return ContentKind.Unknown;
        }

        private static string FormatDateTitle(string containerName)
        {
            var sepa = containerName.Split('-');
            if (sepa == null || sepa.Length != 3)
            {
                return containerName;
            }
            while (sepa[0].Length < 4)
            {
                sepa[0] = "0" + sepa[0];
            }
            while (sepa[1].Length < 2)
            {
                sepa[1] = "0" + sepa[1];
            }
            while (sepa[2].Length < 2)
            {
                sepa[2] = "0" + sepa[2];
            }
            return sepa[0] + "-" + sepa[1] + "-" + sepa[2];
        }

        private static string GetLargeImageResource(Item item)
        {
            var matched = item.Resources
                .FirstOrDefault(res => res.ProtocolInfo != null && res.ProtocolInfo.MimeType == "image/jpeg" && res.ProtocolInfo.DlnaProfileName == DlnaProfileName.JpegLarge);

            if (matched != null)
            {
                return matched.ResourceUrl;
            }

            if (item.Class.StartsWith(Class.ImageItem))
            {
                return item.Resources[0].ResourceUrl;
            }
            return null;
        }

        private static string GetOriginalImageResource(Item item)
        {
            var matched = item.Resources
                .FirstOrDefault(res => res.ProtocolInfo != null && res.ProtocolInfo.MimeType == "image/jpeg" && res.ProtocolInfo.IsOriginalContent);

            if (matched != null)
            {
                return matched.ResourceUrl;
            }

            if (item.Class.StartsWith(Class.ImageItem))
            {
                return item.Resources[0].ResourceUrl;
            }
            return null;
        }

        private static string GetThumbnailResource(Item item)
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

            if (result == null)
            {
                return null;
            }
            return result.ResourceUrl;
        }

        private async Task RetrieveContentsByBrowse(string containerName, string containerId, CancellationTokenSource cancel)
        {
            var res = await BrowseChild(containerName, containerId, cancel, 0).ConfigureAwait(false);
            if (res == null)
            {
                return;
            }
            foreach (var container in res.Reverse())
            {
                DebugUtil.Log("Browse - " + container.Title);
                await RetrieveContentsByBrowse(container.Title, container.Id, cancel).ConfigureAwait(false);
            }
        }

        private async Task<IList<Container>> BrowseChild(string containerName, string containerId, CancellationTokenSource cancel, int start)
        {
            var res = await UpnpDevice.Services[URN.ContentDirectory].Control(new BrowseRequest
            {
                ObjectID = containerId,
                BrowseFlag = BrowseFlag.BrowseDirectChildren,
                StartingIndex = start,
                RequestedCount = CONTENT_LOOP_STEP,
            }).ConfigureAwait(false);
            if (cancel != null && cancel.IsCancellationRequested)
            {
                OnCancelled();
                return null;
            }

            var contents = res as RetrievedContents;
            OnPartLoaded(Translate(containerName, contents.Result.Items));

            var containers = new List<Container>(contents.Result.Containers);

            if (contents.TotalMatches > (start + 1) * CONTENT_LOOP_STEP)
            {
                var nextContainers = await BrowseChild(containerName, containerId, cancel, start + 1).ConfigureAwait(false);
                containers.AddRange(nextContainers);
            }
            return containers;
        }
    }
}
