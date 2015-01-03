#if DEBUG
using Kazyx.RemoteApi.AvContent;
using Kazyx.Uwpmm.DataModel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#endif

namespace Kazyx.Uwpmm.Playback
{
#if DEBUG
    public class DummyContentsLoader : ContentsLoader
    {
        private readonly Random random;
        public DummyContentsLoader()
        {
            random = new Random();
        }

        public override async Task Load(CancellationTokenSource cancel)
        {
            var CurrentUuid = DummyContentsLoader.RandomUuid();

            foreach (var date in RandomDateList(10))
            {
                await Task.Delay(500).ConfigureAwait(false);

                if (cancel != null && cancel.IsCancellationRequested)
                {
                    OnCancelled();
                    break;
                }

                var list = new List<Thumbnail>();
                foreach (var content in RandomContentList(30))
                {
                    content.GroupName = date.Title;
                    list.Add(new Thumbnail(content, CurrentUuid));
                }

                OnPartLoaded(list);
            }

            OnCompleted();
        }

        private IList<DateInfo> RandomDateList(int count)
        {
            var list = new List<DateInfo>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new DateInfo
                {
                    Title = YMDwithPadding(),
                    Uri = "dummyuri",
                });
                list.Sort((d1, d2) => { return string.CompareOrdinal(d2.Title, d1.Title); });
            }
            return list;
        }

        private IList<ContentInfo> RandomContentList(int count)
        {
            var list = new List<ContentInfo>();
            for (int i = 0; i < random.Next(1, count); i++)
            {
                list.Add(new ContentInfo
                {
                    ContentType = ContentType(),
                    ThumbnailUrl = ThumbnailUrl(),
                    Name = FileName(),
                    CreatedTime = CreatedTime(),
                    LargeUrl = "http://upload.wikimedia.org/wikipedia/commons/e/e5/Earth_.jpg",
                    Protected = Protected(),
                });
            }
            return list;
        }

        private bool Protected()
        {
            return random.Next(0, 100) > 80; // protected is 20%.
        }

        public static string RandomUuid()
        {
            return "uuid:" + Guid.NewGuid().ToString();
        }

        private static readonly string[] dummyimages = new string[]{
            "http://cdn.gsmarena.com/vv/newsimg/13/12/htc-one-max-black/gsmarena_001.jpg",
            "http://www.notebookcheck.net/fileadmin/_processed_/csm_Nokia-Lumia-720-3__2__d354fb1d00.jpg",
            "http://www.technobuffalo.com/wp-content/uploads/2013/05/Verizon-Nokia-Lumia-928-VS-Nokia-Lumia-920-Front.jpg",
            "http://www.sony.jp/products/picture/ILCE-QX1_SELP1650.jpg",
        };

        private static string CreatedTime()
        {
            return DateTimeOffset.Now.ToString("yyyy-MM-ddThh:mm:ssZ");
        }

        private string FileName()
        {
            return "DUMMYFILE_" + random.Next(0, 10000);
        }

        private string ThumbnailUrl()
        {
            return dummyimages[random.Next(0, dummyimages.Length - 1)];
        }

        private string ContentType()
        {
            return random.NextDouble() > 0.1 ? ContentKind.StillImage : ContentKind.MovieMp4;
        }

        private int Year()
        {
            return random.Next(2000, 2014);
        }

        private int Month()
        {
            return random.Next(1, 12);
        }

        private int Day()
        {
            return random.Next(1, 28);
        }

        private string YMDwithPadding()
        {
            var m = Month().ToString();
            if (m.Length == 1)
            {
                m = "0" + m;
            }
            var d = Day().ToString();
            if (d.Length == 1)
            {
                d = "0" + d;
            }
            return Year().ToString() + m + d;
        }
    }
#endif
}
