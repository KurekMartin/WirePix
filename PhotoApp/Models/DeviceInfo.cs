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
        public string ID { get; } = string.Empty;
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
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastBackup
        {
            get => _lastBackup;
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
            get => _connected;
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
