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
using Windows.UI.Xaml.Media.Imaging;

namespace Kazyx.Uwpmm.DataModel
{
    public class Thumbnail : ObservableBase
    {
        public Thumbnail(ContentInfo content, string uuid)
        {
            GroupTitle = content.GroupName;
            Source = content;
            DeviceUuid = uuid;
        }

        public Thumbnail(ContentInfo content, StorageFile localfile)
        {
            GroupTitle = content.GroupName;
            CacheFile = localfile;
            Source = content;
            DeviceUuid = "localhost";
        }

        public ContentInfo Source { private set; get; }

        public bool IsMovie
        {
            get
            {
                switch (Source.ContentType)
                {
                    case ContentKind.MovieMp4:
                    case ContentKind.MovieXavcS:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private SelectivityFactor factor = SelectivityFactor.None;
        public SelectivityFactor SelectivityFactor
        {
            set
            {
                factor = value;
                NotifyChanged("IsSelectable");
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
                        return !IsMovie;
                    case SelectivityFactor.Delete:
                        return !Source.Protected;
                    default:
                        throw new NotImplementedException("Unknown SelectivityFactor");
                }
            }
        }

        private string DeviceUuid { set; get; }

        public string GroupTitle { private set; get; }

        public StorageFile CacheFile { private set; get; }

        private async Task LoadCachedThumbnailImageAsync(ImageMode mode)
        {
            var file = CacheFile;
            if (file == null)
            {
                DebugUtil.Log("CacheFile is null");
                return;
            }

            try
            {
                using (var stream = await file.GetThumbnailAsync(ThumbnailMode.ListView))
                {
                    var bmp = new BitmapImage();
                    bmp.CreateOptions = BitmapCreateOptions.None;
                    await bmp.SetSourceAsync(stream);

                    if (ImageMode.Image == mode) { ThumbnailImage = bmp; }
                    else { LargeImage = bmp; }
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
            get { return GetImage(ImageMode.Image); }
        }

        private BitmapImage _LargeImage = null;

        public BitmapImage LargeImage
        {
            private set
            {
                _LargeImage = value;
                NotifyChanged("LargeImage");
            }
            get { return GetImage(ImageMode.Album); }
        }

        private BitmapImage GetImage(ImageMode mode)
        {
            BitmapImage tmp = null;
            if (ImageMode.Image == mode)
            {
                tmp = Interlocked.Exchange(ref _ThumbnailImage, null);
            }
            else
            {
                tmp = Interlocked.Exchange(ref _LargeImage, null);
            }
            if (tmp != null)
            {
                return tmp;
            }

            if (CacheFile == null)
            {
                var task = FetchThumbnailAsync().ConfigureAwait(false);
            }
            else
            {
                var task = LoadCachedThumbnailImageAsync(mode).ConfigureAwait(false);
            }

            return null;
        }

        private enum ImageMode
        {
            Image,
            Album,
        }

        private async Task FetchThumbnailAsync()
        {
            try
            {
                var file = await ThumbnailCacheLoader.INSTANCE.LoadCacheFileAsync(DeviceUuid, Source);
                CacheFile = file;
                await LoadCachedThumbnailImageAsync(ImageMode.Image);
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

    public class Album : List<Thumbnail>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        public string Key { private set; get; }

        private Thumbnail Thumb;

        public BitmapImage RandomThumbnail
        {
            get
            {
                if (Thumb == null)
                {
                    Thumb = this[new Random().Next(0, Count - 1)];
                    Thumb.PropertyChanged += Thumb_PropertyChanged;
                }
                return Thumb.LargeImage;
            }
        }

        void Thumb_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LargeImage")
            {
                OnPropertyChanged("RandomThumbnail");
            }
        }

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

        public Album(string key)
        {
            Key = key;
        }

        new public void Add(Thumbnail content)
        {
            var previous = Count;
            base.Add(content);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, content, previous));
        }

        new public bool Remove(Thumbnail content)
        {
            var index = IndexOf(content);
            var removed = base.Remove(content);
            if (removed)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, content, index));
            }
            return removed;
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

    public class AlbumGroupCollection : ObservableCollection<Album>
    {
        private readonly bool SortAlbum;

        public AlbumGroupCollection(bool sortAlbum = true)
        {
            SortAlbum = sortAlbum;
        }

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

        public bool Remove(Thumbnail content)
        {
            var group = GetGroup(content.GroupTitle);
            if (group == null)
            {
                DebugUtil.Log("Remove: group does not exist");
                return false;
            }
            return group.Remove(content);
        }

        public void Add(Thumbnail content)
        {
            var group = GetGroup(content.GroupTitle);
            if (group == null)
            {
                group = new Album(content.GroupTitle);
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
                    g = new Album(group.Key);
                    SortAdd(g);
                }
                g.AddRange(group.Value);
            }
        }

        private void SortAdd(Album item)
        {
            int insertAt = Items.Count;
            if (SortAlbum)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    if (string.CompareOrdinal(Items[i].Key, item.Key) < 0)
                    {
                        insertAt = i;
                        break;
                    }
                }
            }
            Insert(insertAt, item);
        }

        private Album GetGroup(string key)
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
