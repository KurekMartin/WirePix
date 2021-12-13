using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoApp.Models
{
    public class DateRange : BaseObserveObject
    {
        private DateTime _start;
        private DateTime _end;
        public DateRange()
        {
            Start = End = DateTime.Now.Date;
        }
        public DateTime Start
        {
            get
            {
                return _start;
            }
            set
            {
                if (value != _start)
                {
                    _start = value;
                    OnPropertyChanged();
                }
            }
        }
        public DateTime End
        {
            get
            {
                return _end;
            }
            set
            {
                if (value != _end)
                {
                    _end = value;
                    OnPropertyChanged();
                }
            }
        }

        public static bool operator ==(DateRange dr1, DateRange dr2)
        {
            return (dr1.Start == dr2.Start && dr1.End == dr2.End);
        }
        public static bool operator !=(DateRange dr1, DateRange dr2)
        {
            return (dr1.Start != dr2.Start || dr1.End != dr2.End);
        }
    }
}
