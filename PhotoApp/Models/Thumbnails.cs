using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoApp.Models
{
    public class Thumbnails:BaseObserveObject
    {
        private ThumbnailSelect _selected;
        private int _value = 0;
        public ThumbnailSelect Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                if(value!=_selected)
                {
                    _selected = value;
                    OnPropertyChanged();
                }
            }
        }
        public int Value {
            get
            {
                return _value;
            }
            set
            {
                if(value!=_value)
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
