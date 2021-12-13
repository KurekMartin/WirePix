using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PhotoApp.Models
{
    public class DeviceInfo : BaseObserveObject
    {
        private string _name = string.Empty;
        private bool _connected = false;
        private DateTime _lastBackup = new DateTime();
        public DeviceInfo() { }
        public DeviceInfo(string name, string id, DateTime lastBackup = new DateTime(), bool connected = false)
        {
            _name = name;
            ID = id;
            LastBackup = lastBackup;
            _connected = connected;
        }
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }
        public string ID { get; set; }
        public DateTime LastBackup
        {
            get { return _lastBackup; }
            set
            {
                if (value != _lastBackup)
                {
                    _lastBackup = value;
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore]
        public bool Connected
        {
            get { return _connected; }
            set
            {
                if (value != _connected)
                {
                    _connected = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
