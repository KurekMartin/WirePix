using MediaDevices;
using PhotoApp.Models;
using System;
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
        private ObservableCollection<Device> _devices = new ObservableCollection<Device>();
        private static readonly string _dataFile = Path.Combine(Application.Current.Resources[Properties.Keys.DataFolder].ToString(), "Devices.xml");
        private bool _isListUpdated = false;
        private string _selectedDeviceID;

        public void Save()
        {
            foreach (var device in _devices)
            {
                Database.DeviceSetCustomName(origName: device.Name, serialNum: device.SerialNumber, customName: device.CustomName);
            }
        }

        public int UpdateDevices(string selectedDeviceID = "")
        {
            IEnumerable<MediaDevice> connectedDevices = MediaDevice.GetDevices();
            //select only new devices that support MTP or PTP
            IEnumerable<MediaDevice> MTPDevices = connectedDevices.Where(d =>
                  {
                      bool isMediaDevice = false;
                      if (!_devices.Any(c => d.DeviceId == c.ID))
                      {
                          d.Connect();
                          isMediaDevice = d.Protocol.ToUpper().Contains("MTP") || d.Protocol.ToUpper().Contains("PTP");
                          d.Disconnect();
                      }
                      else { isMediaDevice = true; }
                      return isMediaDevice;
                  });

            IEnumerable<string> deviceIDs = Devices.Select(d => d.ID);
            IEnumerable<MediaDevice> newDevices = MTPDevices.Where(d => !deviceIDs.Contains(d.DeviceId));

            int devicesAdded = newDevices.Count();
            int devicesRemoved = RemoveDisconnectedDevices(MTPDevices);

            foreach (MediaDevice mediaDevice in newDevices)
            {
                Device device = new Device(mediaDevice);

                if (!Database.DeviceExists(device.Name, device.SerialNumber))
                {
                    Database.DeviceInsert(device.Name, device.SerialNumber);
                }
                var deviceData = Database.DeviceGetCustomName(device.Name, device.SerialNumber);
                device.CustomName = deviceData.customName;
                device.LastBackup = deviceData.lastBackup;
                Devices.Add(device);
                Devices = new ObservableCollection<Device>(Devices.OrderBy(d => d.Name));
            }

            var selectedDevice = Devices.FirstOrDefault(d => d.ID == selectedDeviceID);
            int index = Devices.IndexOf(selectedDevice);

            _selectedDeviceID = string.Empty;
            if (index >= 0)
            {
                _selectedDeviceID = selectedDeviceID;
            }
            else if (Devices.Count() > 0)
            {
                _selectedDeviceID = Devices.First().ID;
                index = 0;
            }
            return index;
        }

        private int RemoveDisconnectedDevices(IEnumerable<MediaDevice> connectedDevices)
        {
            var devicesToRemove = Devices.Where(device => !connectedDevices.Any(conDev => device.ID == conDev.DeviceId)).ToList();
            foreach (var device in devicesToRemove)
            {
                Devices.Remove(device);
            }
            return devicesToRemove.Count();
        }

        public ObservableCollection<Device> Devices
        {
            get => _devices;
            private set
            {
                _devices = value;
                OnPropertyChanged();
            }
        }

        public Device SelectDeviceByID(string id)
        {
            var device = Devices.First(d => d.ID == id);
            if (device != null)
            {
                _selectedDeviceID = id;
                OnPropertyChanged(nameof(SelectedDevice));
            }
            return device;
        }

        public Device SelectedDevice
        {
            get
            {
                if (!string.IsNullOrEmpty(_selectedDeviceID))
                {
                    return Devices.First(d => d.ID == _selectedDeviceID);
                }
                return null;
            }
        }
    }
}
