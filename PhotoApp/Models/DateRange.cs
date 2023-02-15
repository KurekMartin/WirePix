using System;

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
            get => _start;
            set
            {
                if (value != _start)
                {
                    _start = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DateRange));
                }
            }
        }
        public DateTime End
        {
            get => _end;
            set
            {
                if (value != _end)
                {
                    _end = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DateRange));
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
