using Kazyx.RemoteApi.AvContent;
using Kazyx.Uwpmm.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Kazyx.Uwpmm.DataModel
{
    public class PlaybackSource : ObservableBase
    {
        public PlaybackSource(string uuid, DateInfo date, ContentInfo content)
        {
            GroupTitle = date.Title;
            Source = content;
            DeviceUuid = uuid;
        }

        public ContentInfo Source { private set; get; }

        public Visibility ProtectedIconVisibility
        {
            get
            {
                return Source.Protected ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility MovieIconVisibility
        {
            get
            {
                switch (Source.ContentType)
                {
                    case ContentKind.MovieMp4:
                    case ContentKind.MovieXavcS:
                        return Visibility.Visible;
                    default:
                        return Visibility.Collapsed;
                }
            }
        }

        public Visibility CopyToPhoneVisibility
        {
            get { if (MovieIconVisibility == Visibility.Collapsed) { return Visibility.Visible; } else { return Visibility.Collapsed; } }
        }

        public Visibility DeleteMenuVisiblity
        {
            get { if (ProtectedIconVisibility == Visibility.Collapsed) { return Visibility.Visible; } else { return Visibility.Collapsed; } }
        }

        public Visibility UnselectableMaskVisibility
        {
            get
            {
                return IsSelectable ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private SelectivityFactor factor = SelectivityFactor.None;
        public SelectivityFactor SelectivityFactor
        {
            set
            {
                factor = value;
                NotifyChanged("IsSelectable");
                NotifyChanged("UnselectableMaskVisibility");
            }
            get { return factor; }
        }

        public bool IsSelectable
        {
            get
            {
                switch (SelectivityFactor)
                {
                    case SelectivityFactor.None:
                        return true;
                    case SelectivityFactor.CopyToPhone:
                        return MovieIconVisibility == Visibility.Collapsed;
                    case SelectivityFactor.Delete:
                        return ProtectedIconVisibility == Visibility.Collapsed;
                    default:
                        throw new NotImplementedException("Unknown SelectivityFactor");
                }
            }
            //get { return MovieIconVisibility == Visibility.Collapsed; }
        }

        private string DeviceUuid { set; get; }

        public string GroupTitle { private set; get; }

        private StorageFile _CacheFile = null;
        public StorageFile CacheFile
        {
            set
            {
                _CacheFile = value;
                NotifyChangedOnUI("CacheFile");
            }
            get { return _CacheFile; }
        }

        private void LoadCachedThumbnailImageAsync()
        {
            var file = CacheFile;

            LoaderTask = Task.Run(async () =>
            {
                try
                {
                    if (file == null)
                    {
                        return null;
                    }

                    var bmp = new BitmapImage();
                    bmp.CreateOptions = BitmapCreateOptions.None;

                    using (var stream = await file.GetThumbnailAsync(ThumbnailMode.PicturesView))
                    {
                        bmp.SetSource(stream);
                    }

                    return bmp;
                }
                catch (Exception e)
                {
                    DebugUtil.Log(e.StackTrace);
                    // CacheFile seems to be deleted.
                    CacheFile = null;
                    return null;
                }
            });

            var scheduler = (SynchronizationContext.Current == null) ? TaskScheduler.Current : TaskScheduler.FromCurrentSynchronizationContext();
            LoaderTask.ContinueWith((t) =>
            {
                if (!t.IsCanceled && !t.IsFaulted)
                {
                    NotifyChangedOnUI("ThumbnailImage");
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, scheduler);
        }

        private Task<BitmapImage> LoaderTask { get; set; }

        public BitmapImage ThumbnailImage
        {
            get
            {
                if (LoaderTask != null)
                {
                    if (LoaderTask.Status == TaskStatus.RanToCompletion)
                    {
                        var image = LoaderTask.Result;
                        LoaderTask = null;
                        return image;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    LoadCachedThumbnailImageAsync();
                    return null;
                }
            }
        }

        public async Task FetchThumbnailAsync()
        {
            if (CacheFile != null)
            {
                return;
            }

            try
            {
                CacheFile = await ThumbnailCacheLoader.INSTANCE.LoadCacheFileAsync(DeviceUuid, Source);
            }
            catch (Exception e)
            {
                DebugUtil.Log(e.StackTrace);
                DebugUtil.Log("Failed to fetch thumbnail image: " + Source.ThumbnailUrl);
            }
        }
    }

    public enum SelectivityFactor
    {
        None,
        CopyToPhone,
        Delete,
    }

    public class DateGroup : List<PlaybackSource>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        public string Key { private set; get; }

        public SelectivityFactor SelectivityFactor
        {
            set
            {
                foreach (var thumb in this)
                {
                    thumb.SelectivityFactor = value;
                }
            }
        }

        public DateGroup(string key)
        {
            Key = key;
        }

        new public void Add(PlaybackSource content)
        {
            var previous = Count;
            base.Add(content);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, content, previous));
        }

        new public void AddRange(IEnumerable<PlaybackSource> contents)
        {
            var previous = Count;
            var list = new List<PlaybackSource>(contents);
            base.AddRange(contents);
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            OnCollectionChanged(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            if (CollectionChanged != null)
            {
                try
                {
                    CollectionChanged(this, e);
                }
                catch (System.NotSupportedException)
                {
                    NotifyCollectionChangedEventArgs alternativeEventArgs =
                        new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    OnCollectionChanged(alternativeEventArgs);
                }
            }
        }
    }

    public class DateGroupCollection : ObservableCollection<DateGroup>
    {
        private SelectivityFactor _SelectivityFactor = SelectivityFactor.None;
        public SelectivityFactor SelectivityFactor
        {
            get { return _SelectivityFactor; }
            set
            {
                _SelectivityFactor = value;
                foreach (var group in this)
                {
                    group.SelectivityFactor = value;
                }
            }
        }

        public void Add(PlaybackSource content)
        {
            var group = GetGroup(content.GroupTitle);
            if (group == null)
            {
                group = new DateGroup(content.GroupTitle);
                SortAdd(group);
            }
            group.Add(content);
        }

        public void AddRange(IEnumerable<PlaybackSource> contents)
        {
            var groups = new Dictionary<string, List<PlaybackSource>>();
            foreach (var content in contents)
            {
                if (!groups.ContainsKey(content.GroupTitle))
                {
                    groups.Add(content.GroupTitle, new List<PlaybackSource>());
                }
                groups[content.GroupTitle].Add(content);
            }

            foreach (var group in groups)
            {
                var g = GetGroup(group.Key);
                if (g == null)
                {
                    g = new DateGroup(group.Key);
                    SortAdd(g);
                }
                g.AddRange(group.Value);
            }
        }

        private void SortAdd(DateGroup item)
        {
            int insertAt = Items.Count;
            for (int i = 0; i < Items.Count; i++)
            {
                if (string.CompareOrdinal(Items[i].Key, item.Key) < 0)
                {
                    insertAt = i;
                    break;
                }
            }
            Insert(insertAt, item);
        }

        private DateGroup GetGroup(string key)
        {
            foreach (var item in base.Items)
            {
                if (item.Key == key)
                {
                    return item;
                }
            }
            return null;
        }
    }
}
