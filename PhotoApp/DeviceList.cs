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
        private List<Device> _devices = new List<Device>();
        private static readonly string _dataFile = Path.Combine(Application.Current.Resources[Properties.Keys.DataFolder].ToString(), "Devices.xml");
        private bool _isListUpdated = false;
        private string _selectedDeviceID;

        public void Save()
        {
            foreach (var device in _devices)
            {
                Database.DeviceEditCustomName(origName: device.Name, serialNum: device.SerialNumber, customName: device.CustomName);
            }
        }

        public int UpdateDevices(string selectedDeviceID = "")
        {
            IEnumerable<MediaDevice> connectedDevices = MediaDevice.GetDevices();
            //select only new devices that support MTP or PTP
            IEnumerable<MediaDevice> devices = connectedDevices.Where(d =>
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
            IEnumerable<MediaDevice> newDevices = devices.Where(d => !deviceIDs.Contains(d.DeviceId));

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
                _devices.Add(device);
                _devices.OrderByDescending(d => d.Name);
                OnPropertyChanged(nameof(Devices));
            }
            int index = Devices.FindIndex(d => d.ID == selectedDeviceID);
            if (index >= 0)
            {
                _selectedDeviceID = selectedDeviceID;
            }
            return index;
        }

        public List<Device> Devices
        {
            get => _devices;
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
