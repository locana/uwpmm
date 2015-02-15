using Kazyx.RemoteApi.AvContent;
using Kazyx.Uwpmm.Playback;
using Kazyx.Uwpmm.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

namespace Kazyx.Uwpmm.DataModel
{
    public class RemainingContentsHolder : Thumbnail
    {
        public RemainingContentsHolder(DateInfo date, string uuid, int startsFrom, int count)
            : base(new ContentInfo { GroupName = date.Title }, uuid)
        {
            StartsFrom = startsFrom;
            RemainingCount = count;
            AlbumGroup = date;
        }

        public RemainingContentsHolder(string containerId, string groupTitle, string uuid, int startsFrom, int count)
            : base(new ContentInfo { GroupName = groupTitle }, uuid)
        {
            StartsFrom = startsFrom;
            RemainingCount = count;
            CdsContainerId = containerId;
        }

        public int StartsFrom { private set; get; }
        public int RemainingCount { private set; get; }
        public DateInfo AlbumGroup { private set; get; }
        public string CdsContainerId { private set; get; }

        public override string OverlayText
        {
            get
            {
                if (RemainingCount == 0) { return null; }
                else { return "+" + RemainingCount; }
            }
        }

        public override bool IsSelectable
        {
            get
            {
                switch (SelectivityFactor)
                {
                    case SelectivityFactor.None:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public override bool IsMovie { get { return false; } }
        public override bool IsDeletable { get { return false; } }
        public override bool IsPlayable { get { return false; } }
        public override bool IsCopyable { get { return false; } }
        public override bool IsContent { get { return false; } }
        public override BitmapImage ThumbnailImage { get { return null; } }
        public override BitmapImage LargeImage { get { return null; } }
    }

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

        public virtual string OverlayText { get { return null; } }

        public virtual bool IsMovie
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

        public virtual bool IsSelectable
        {
            get
            {
                switch (SelectivityFactor)
                {
                    case SelectivityFactor.None:
                        return true;
                    case SelectivityFactor.CopyToPhone:
                        return IsCopyable;
                    case SelectivityFactor.Delete:
                        return !Source.Protected;
                    default:
                        throw new NotImplementedException("Unknown SelectivityFactor");
                }
            }
        }

        public virtual bool IsDeletable { get { return !Source.Protected; } }

        public virtual bool IsCopyable
        {
            get
            {
                switch (Source.ContentType)
                {
                    case ContentKind.StillImage:
                    case ContentKind.MovieMp4:
                        return true;
#if WINDOWS_APP
                    case ContentKind.MovieXavcS:
                        // XAVC S is not supported on phone.
                        return true;
#endif
                    default:
                        return false;
                }
            }
        }

        public virtual bool IsPlayable { get { return true; } }

        public virtual bool IsContent { get { return true; } }

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

        public virtual BitmapImage ThumbnailImage
        {
            private set
            {
                _ThumbnailImage = value;
                NotifyChanged("ThumbnailImage");
                NotifyChanged("LargeImage");
            }
            get { return GetImage(ImageMode.Image); }
        }

        private BitmapImage _LargeImage = null;

        public virtual BitmapImage LargeImage
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
                    lock (this)
                    {
                        Thumb = this[new Random().Next(0, Count - 1)];
                    }
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
                lock (this)
                {
                    foreach (var thumb in this)
                    {
                        thumb.SelectivityFactor = value;
                    }
                }
            }
        }

        public Album(string key)
        {
            Key = key;
        }

        new public void Add(Thumbnail content)
        {
            lock (this)
            {
                var previous = Count;
                base.Add(content);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, content, previous));
                if (previous == 0)
                {
                    Thumb = null;
                    OnPropertyChanged("RandomThumbnail");
                }
            }
        }

        new public bool Remove(Thumbnail content)
        {
            lock (this)
            {
                var index = IndexOf(content);
                var removed = base.Remove(content);
                if (removed)
                {
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, content, index));
                }
                return removed;
            }
        }

        new public void AddRange(IEnumerable<Thumbnail> contents)
        {
            lock (this)
            {
                var previous = Count;
                base.AddRange(contents);
                var list = new List<Thumbnail>(contents);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list, previous));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            try
            {
                CollectionChanged.Raise(this, e);
            }
            catch (NotSupportedException)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }

    public class AlbumGroupCollection : List<Album>, INotifyPropertyChanged, INotifyCollectionChanged
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
                lock (this)
                {
                    foreach (var group in this)
                    {
                        group.SelectivityFactor = value;
                    }
                }
            }
        }

        new public void Clear()
        {
            base.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Remove(Thumbnail content, bool deleteGroupIfEmpty = true)
        {
            lock (this)
            {
                var group = GetGroup(content.GroupTitle);
                if (group == null)
                {
                    DebugUtil.Log("Remove: group does not exist");
                    return false;
                }
                var res = group.Remove(content);
                if (deleteGroupIfEmpty && group.Count == 0)
                {
                    DebugUtil.Log("Remove no item group: " + group.Key);
                    var index = IndexOf(group);
                    var removed = Remove(group);
                    if (removed)
                    {
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, group, index));
                    }
                }
                return res;
            }
        }

        public void Add(Thumbnail content)
        {
            lock (this)
            {
                var group = GetGroup(content.GroupTitle);
                if (group == null)
                {
                    group = new Album(content.GroupTitle);
                    SortAdd(group);
                }
                group.Add(content);
            }
        }

        private void SortAdd(Album item)
        {
            int insertAt = Count;
            if (SortAlbum)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (string.CompareOrdinal(this[i].Key, item.Key) < 0)
                    {
                        insertAt = i;
                        break;
                    }
                }
            }
            Insert(insertAt, item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, insertAt));
        }

        private Album GetGroup(string key)
        {
            return this.SingleOrDefault(item => item.Key == key);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            try
            {
                CollectionChanged.Raise(this, e);
            }
            catch (NotSupportedException)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }
}
