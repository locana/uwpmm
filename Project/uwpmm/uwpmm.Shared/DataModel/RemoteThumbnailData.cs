using Kazyx.RemoteApi.AvContent;
using Kazyx.Uwpmm.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Kazyx.Uwpmm.DataModel
{
    public class RemoteThumbnail : ObservableBase
    {
        public RemoteThumbnail(string uuid, DateInfo date, ContentInfo content)
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

        private string _CachePath = null;
        public string CachePath
        {
            set
            {
                _CachePath = value;

                ConverterTask = Task.Run(async () =>
                {
                    try
                    {
                        if (string.IsNullOrEmpty(value))
                        {
                            return null;
                        }

                        var bmp = new BitmapImage();
                        bmp.CreateOptions = BitmapCreateOptions.None;

                        var file = await ApplicationData.Current.TemporaryFolder.GetFileAsync(value);
                        using (var stream = await file.OpenStreamForReadAsync())
                        {
                            bmp.SetSource(stream.AsRandomAccessStream());
                        }

                        return bmp;
                    }
                    catch (Exception e)
                    {
                        DebugUtil.Log(e.StackTrace);
                        return null;
                    }
                });

                var scheduler = (SynchronizationContext.Current == null) ? TaskScheduler.Current : TaskScheduler.FromCurrentSynchronizationContext();
                ConverterTask.ContinueWith((t) =>
                {
                    if (!t.IsCanceled && !t.IsFaulted)
                    {
                        NotifyChangedOnUI("ThumbnailImage");
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, scheduler);
            }
            get
            {
                return _CachePath;
            }
        }

        private Task<BitmapImage> ConverterTask { get; set; }

        public BitmapImage ThumbnailImage
        {
            get
            {
                if (ConverterTask != null)
                {
                    return (ConverterTask.Status == TaskStatus.RanToCompletion) ? ConverterTask.Result : null;
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task FetchThumbnailAsync()
        {
            if (CachePath != null)
            {
                return;
            }

            try
            {
                CachePath = await ThumbnailCacheLoader.INSTANCE.GetCachePathAsync(DeviceUuid, Source);
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

    public class DateGroup : List<RemoteThumbnail>, INotifyPropertyChanged, INotifyCollectionChanged
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

        new public void Add(RemoteThumbnail content)
        {
            var previous = Count;
            base.Add(content);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, content, previous));
        }

        new public void AddRange(IEnumerable<RemoteThumbnail> contents)
        {
            var previous = Count;
            var list = new List<RemoteThumbnail>(contents);
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

        public void Add(RemoteThumbnail content)
        {
            var group = GetGroup(content.GroupTitle);
            if (group == null)
            {
                group = new DateGroup(content.GroupTitle);
                SortAdd(group);
            }
            group.Add(content);
        }

        public void AddRange(IEnumerable<RemoteThumbnail> contents)
        {
            var groups = new Dictionary<string, List<RemoteThumbnail>>();
            foreach (var content in contents)
            {
                if (!groups.ContainsKey(content.GroupTitle))
                {
                    groups.Add(content.GroupTitle, new List<RemoteThumbnail>());
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
