using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PhotoApp.Models
{
    public enum ListMode
    {
        whitelist,
        blacklist
    }
    public class FileTypeSelection : BaseObserveObject
    {
        private ObservableCollection<string> _fileTypes;
        private ListMode _mode = ListMode.whitelist;

        public FileTypeSelection()
        {
            FileTypes = new ObservableCollection<string>();
        }
        public ObservableCollection<string> FileTypes
        {
            get { return _fileTypes; }
            set
            {
                if (value != _fileTypes)
                {
                    _fileTypes = value;
                    _fileTypes.CollectionChanged += fileTypes_CollectionChanged;
                    OnPropertyChanged();
                }
            }
        }

        private void fileTypes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(FileTypes));
        }

        public ListMode Mode
        {
            get { return _mode; }
            set
            {
                if (value != _mode) _mode = value;
                OnPropertyChanged();
            }
        }

    }
}
