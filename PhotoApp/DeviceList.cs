using MediaDevices;
using PhotoApp.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace PhotoApp
{
    public class DeviceList : BaseObserveObject
    {
        [XmlElement]
        public ObservableCollection<DeviceInfo> DeviceInfo = new ObservableCollection<DeviceInfo>();
        [XmlIgnore]
        public IEnumerable<DeviceInfo> ConnectedDevicesInfo { get; set; }
        [XmlIgnore]
        private string _selectedDeviceID;
        [XmlIgnore]
        private List<Device> _devices = new List<Device>();
        [XmlIgnore]
        private static readonly string _dataFile = Path.Combine(Application.Current.Resources[Properties.Keys.DataFolder].ToString(), "Devices.xml");

        public DeviceList()
        {
            DeviceInfo.CollectionChanged += DeviceInfo_CollectionChanged;
            ConnectedDevicesInfo = new List<DeviceInfo>();
        }

        public void Load()
        {
            if (File.Exists(_dataFile))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DeviceList));
                using (FileStream fs = File.OpenRead(_dataFile))
                {
                    DeviceList deviceList = (DeviceList)serializer.Deserialize(fs);
                    DeviceInfo = deviceList.DeviceInfo;
                    fs.Close();
                }
            }
        }

        private void DeviceInfo_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                    item.PropertyChanged -= Item_PropertyChanged;
            }
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                    item.PropertyChanged += Item_PropertyChanged;
            }
            ConnectedDevicesInfo = DeviceInfo.Where(d => d.Connected == true);
            OnPropertyChanged("ConnectedDevicesInfo");
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ConnectedDevicesInfo = DeviceInfo.Where(d => d.Connected == true);
            OnPropertyChanged("ConnectedDevicesInfo");
        }

        public void Save()
        {
            Directory.CreateDirectory(Application.Current.Resources[Properties.Keys.DataFolder].ToString());
            XmlSerializer serializer = new XmlSerializer(typeof(DeviceList));
            using (FileStream fs = File.Create(_dataFile))
            {
                TextWriter writer = new StreamWriter(fs);
                serializer.Serialize(writer, this);
                writer.Close();
                fs.Close();
            }
        }

        public int UpdateDevices()
        {
            IEnumerable<MediaDevice> devices = MediaDevice.GetDevices().Where(d =>
            {
                d.Connect();
                bool isMediaDevice = d.Protocol.ToUpper().Contains("MTP") || d.Protocol.ToUpper().Contains("PTP"); //filtr podle protokolu
                d.Disconnect();
                return isMediaDevice;
            });
            DeviceInfo.Select(d => { d.Connected = false; return d; }).ToList();

            foreach (MediaDevice device in devices)
            {
                var deviceInfo = DeviceInfo.Where(d => d.ID == device.DeviceId);
                if (deviceInfo.Count() == 0)
                {
                    DeviceInfo.Add(new DeviceInfo(device.Description, device.DeviceId, connected: true));
                }
                else
                {
                    var deviceOnline = DeviceInfo.First(d => d.ID == device.DeviceId);
                    deviceOnline.Connected = true;
                }

                if (_devices.FindIndex(d => d.ID == device.DeviceId) == -1)
                {
                    _devices.Add(new Device(device));
                }
            }

            foreach (var device in DeviceInfo.Where(d => !d.Connected))
            {
                _devices.RemoveAll(d => d.ID == device.ID);
            }

            DeviceInfo.OrderByDescending(d => d.Name);
            ConnectedDevicesInfo = DeviceInfo.Where(d => d.Connected == true);
            OnPropertyChanged("ConnectedDevicesInfo");

            if (ConnectedDevicesInfo.Count() > 0 && SelectedDeviceIndex == -1)
            {
                SelectDeviceByIndex(0);
            }
            else
            {
                SelectDeviceByIndex(SelectedDeviceIndex);
            }

            return SelectedDeviceIndex;
        }

        public Device GetDeviceByID(string deviceID)
        {
            return _devices.FirstOrDefault(d => d.ID == deviceID);
        }

        public Device SelectDeviceByIndex(int index)
        {
            if (index > -1 )
            {
                Device device = _devices.FirstOrDefault(d => d.ID == ConnectedDevicesInfo.ElementAt(index).ID);
                _selectedDeviceID = device.ID;
                OnPropertyChanged("SelectedDeviceInfo");
                OnPropertyChanged("SelectedDevice");
                return device;
            }
            return null;
        }

        public Device SelectedDevice
        {
            get
            {
                if (_selectedDeviceID != null)
                {
                    return _devices.First(d => d.ID == _selectedDeviceID);
                }
                return null;
            }
        }

        public DeviceInfo SelectedDeviceInfo
        {
            get
            {
                if (ConnectedDevicesInfo.Count() > 0 && _selectedDeviceID != null)
                {
                    return ConnectedDevicesInfo.First(d => d.ID == _selectedDeviceID);
                }
                else
                    return new DeviceInfo();

            }
        }

        public int SelectedDeviceIndex
        {
            get
            {
                if (_selectedDeviceID != null)
                {
                    return ConnectedDevicesInfo.ToList().FindIndex(d => d.ID == _selectedDeviceID);
                }
                return -1;
            }
        }

        public IEnumerable<Device> Devices
        {
            get { return _devices; }
        }
    }
}
