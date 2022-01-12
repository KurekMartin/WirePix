namespace PhotoApp.Models
{
    public class Thumbnails : BaseObserveObject
    {
        private ThumbnailSelect _selected;
        private int _value = 0;
        public ThumbnailSelect Selected
        {
            get => _selected;
            set
            {
                if (value != _selected)
                {
                    _selected = value;
                    OnPropertyChanged();
                }
            }
        }
        public int Value
        {
            get => _value;
            set
            {
                if (value != _value)
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }
    }

    public enum ThumbnailSelect
    {
        longerSide,
        shorterSide
    }
}
