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
    public class Thumbnail : ObservableBase
    {
        public Thumbnail(string uuid, DateInfo date, ContentInfo content)
        {
            GroupTitle = date.Title;
            Source = content;
            DeviceUuid = uuid;
        }

        public Thumbnail(string groupTitle, StorageFile localfile, ContentInfo content)
        {
            GroupTitle = groupTitle;
            CacheFile = localfile;
            Source = content;
            DeviceUuid = "localhost";
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
                NotifyChangedOnUI("ThumbnailImage");
            }
            get { return _CacheFile; }
        }

        private async void LoadCachedThumbnailImageAsync()
        {
            var file = CacheFile;
            if (file == null)
            {
                DebugUtil.Log("CacheFile is null");
                return;
            }

            try
            {
                using (var stream = await file.GetThumbnailAsync(ThumbnailMode.PicturesView))
                {
                    DebugUtil.Log("Set source async.");
                    await SystemUtil.GetCurrentDispatcher().RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        var bmp = new BitmapImage();
                        bmp.CreateOptions = BitmapCreateOptions.None;
                        await bmp.SetSourceAsync(stream);
                        ThumbnailImage = bmp;
                    });
                }
            }
            catch { DebugUtil.Log("Failed to load thumbnail from cache."); }
        }

        private BitmapImage _ThumbnailImage = null;

        public BitmapImage ThumbnailImage
        {
            private set
            {
                _ThumbnailImage = value;
                NotifyChanged("ThumbnailImage");
            }
            get
            {
                var tmp = Interlocked.Exchange(ref _ThumbnailImage, null);
                if (tmp != null)
                {
                    DebugUtil.Log("Return loaded BitmapImage.");
                    return tmp;
                }
                if (CacheFile == null)
                {
                    FetchThumbnailAsync();
                    return null;
                }
                else
                {
                    DebugUtil.Log("Load BitmapImage from cache async.");
                    LoadCachedThumbnailImageAsync();
                    return null;
                }
            }
        }

        private async void FetchThumbnailAsync()
        {
            try
            {
                DebugUtil.Log("Trying to fetch thumbnail image");
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

    public class DateGroup : List<Thumbnail>, INotifyPropertyChanged, INotifyCollectionChanged
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

        new public void Add(Thumbnail content)
        {
            var previous = Count;
            base.Add(content);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, content, previous));
        }

        new public void AddRange(IEnumerable<Thumbnail> contents)
        {
            var previous = Count;
            var list = new List<Thumbnail>(contents);
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

        public void Add(Thumbnail content)
        {
            var group = GetGroup(content.GroupTitle);
            if (group == null)
            {
                group = new DateGroup(content.GroupTitle);
                SortAdd(group);
            }
            group.Add(content);
        }

        public void AddRange(IEnumerable<Thumbnail> contents)
        {
            var groups = new Dictionary<string, List<Thumbnail>>();
            foreach (var content in contents)
            {
                if (!groups.ContainsKey(content.GroupTitle))
                {
                    groups.Add(content.GroupTitle, new List<Thumbnail>());
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
