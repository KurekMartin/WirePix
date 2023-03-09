using System;
using System.Data.SQLite;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace PhotoApp
{
    public static class Database
    {
        private static string _dataFolder = App.Current.Resources[Properties.Keys.DataFolder].ToString();
        private static string _databaseName = Path.Combine(_dataFolder, "db.sqlite");
        private static string _connectionString = @"URI=file:" + _databaseName;
        public static void Create()
        {
            Version = 1;
            if (!File.Exists(_databaseName))
            {
                SQLiteConnection.CreateFile(_databaseName);
            }
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Devices(
                                            OrigName TEXT NOT NULL,
                                            Manufacturer TEXT NOT NULL,
                                            SerialNum TEXT NOT NULL, 
                                            LastBackup TEXT,
                                            CustomName TEXT,
                                            CustomType TEXT,                                            
                                            PRIMARY KEY(OrigName, Manufacturer, SerialNum))";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Files(
                                            Id INTEGER PRIMARY KEY, 
                                            Hash TEXT, 
                                            DevicePath TEXT,
                                            DateTaken TEXT,
                                            LastWriteTime TEXT,
                                            PersistentUID TEXT,
                                            Downloaded INTEGER NOT NULL)";
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static void Connect()
        {
            Create();
        }

        public static long Version
        {
            get
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(connection))
                    {
                        cmd.CommandText = @"PRAGMA user_version";
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            return reader.GetInt64(0);
                        }
                    }
                }
            }
            private set
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(connection))
                    {
                        cmd.CommandText = $@"PRAGMA user_version = {value}";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public static bool DeviceExists(string origName, string manufacturer, string serialNum)
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"SELECT * FROM Devices 
                                        WHERE 
                                            OrigName = ($origName) 
                                        AND Manufacturer = ($manufacturer)
                                        AND SerialNum = ($serialNum)";
                    cmd.Parameters.AddWithValue("$origName", origName);
                    cmd.Parameters.AddWithValue("$manufacturer", manufacturer);
                    cmd.Parameters.AddWithValue("$serialNum", serialNum);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.HasRows;
                    }
                }
            }
        }

        public static void InsertDevice(DBDeviceInfo deviceInfo)
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"INSERT INTO Devices(OrigName, CustomName, CustomType, Manufacturer, SerialNum, LastBackup) 
                                        VALUES ($origName, $customName, $customType, $manufacturer, $serialNum, $lastBackup)";
                    cmd.Parameters.AddWithValue("$origName", deviceInfo.OrigName);
                    cmd.Parameters.AddWithValue("$customName", deviceInfo.CustomName);
                    cmd.Parameters.AddWithValue("$customType", deviceInfo.CustomType);
                    cmd.Parameters.AddWithValue("$manufacturer", deviceInfo.Manufacturer);
                    cmd.Parameters.AddWithValue("$serialNum", deviceInfo.SerialNum);
                    cmd.Parameters.AddWithValue("$lastBackup", deviceInfo.LastBackup.ToString());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static DBDeviceInfo GetDevice(string origName, string manufacturer, string serialNum)
        {
            DBDeviceInfo deviceInfo = new DBDeviceInfo();
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"SELECT * FROM Devices 
                                        WHERE 
                                            OrigName = ($origName) 
                                        AND Manufacturer = ($manufacturer)
                                        AND SerialNum = ($serialNum)";
                    cmd.Parameters.AddWithValue("$origName", origName);
                    cmd.Parameters.AddWithValue("$manufacturer", manufacturer);
                    cmd.Parameters.AddWithValue("$serialNum", serialNum);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            deviceInfo = new DBDeviceInfo()
                            {
                                OrigName = origName,
                                CustomName = reader.GetFieldValue<string>(reader.GetOrdinal("CustomName")),
                                CustomType = reader.GetFieldValue<string>(reader.GetOrdinal("CustomType")),
                                Manufacturer = manufacturer,
                                SerialNum = serialNum,
                                LastBackup = DateTime.Parse(reader.GetFieldValue<string>(reader.GetOrdinal("LastBackup")))
                            };
                        }
                    }
                }
            }
            return deviceInfo;
        }

        public static void DeviceSetCustomName(string origName, string manufacturer, string serialNum, string customName)
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"UPDATE Devices
                                        SET CustomName = ($customName)
                                        WHERE 
                                            OrigName = ($origName)
                                        AND Manufacturer = ($manufacturer) 
                                        AND SerialNum = ($serialNum)";
                    cmd.Parameters.AddWithValue("$customName", customName);
                    cmd.Parameters.AddWithValue("$origName", origName);
                    cmd.Parameters.AddWithValue("$manufacturer", manufacturer);
                    cmd.Parameters.AddWithValue("$serialNum", serialNum);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static bool FileInfoExists(string persistentUID, string fullPath)
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"SELECT * FROM Files
                                        WHERE
                                            PersistentUID = ($persistentUID)
                                        AND DevicePath = ($devicePath)";
                    cmd.Parameters.AddWithValue("$persistentUID", persistentUID);
                    cmd.Parameters.AddWithValue("$devicePath", fullPath);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.HasRows;
                    }
                }
            }
        }

        public static bool FileDownloaded(string persistentUID, string fullPath)
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"SELECT * FROM Files
                                        WHERE
                                            PersistentUID = ($persistentUID)
                                        AND DevicePath = ($devicePath)";
                    cmd.Parameters.AddWithValue("$persistentUID", persistentUID);
                    cmd.Parameters.AddWithValue("$devicePath", fullPath);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            return reader.GetFieldValue<int>(reader.GetOrdinal("Downloaded")) != 0;
                        }
                        return false;
                    }
                }
            }
        }

        public static DBFileInfo GetFileInfo(string persistentUID, string fullPath)
        {
            DBFileInfo fileInfo = null;
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"SELECT * FROM Files
                                        WHERE
                                            PersistentUID = ($persistentUID)
                                        AND DevicePath = ($devicePath)";
                    cmd.Parameters.AddWithValue("$persistentUID", persistentUID);
                    cmd.Parameters.AddWithValue("$devicePath", fullPath);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fileInfo = new DBFileInfo()
                            {
                                Hash = reader.GetFieldValue<string>(reader.GetOrdinal("Hash")),
                                DevicePath = reader.GetFieldValue<string>(reader.GetOrdinal("DevicePath")),
                                PersistentUID = reader.GetFieldValue<string>(reader.GetOrdinal("PersistentUID")),
                                DateTaken = DateTime.Parse(reader.GetFieldValue<string>(reader.GetOrdinal("DateTaken"))),
                                LastWriteTime = DateTime.Parse(reader.GetFieldValue<string>(reader.GetOrdinal("LastWriteTime"))),
                                Downloaded = reader.GetFieldValue<int>(reader.GetOrdinal("Downloaded")) != 0
                            };
                        }
                    }
                }
            }
            return fileInfo;
        }

        public static void InsertFileInfo(DBFileInfo fileInfo)
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"INSERT INTO Files(Hash, PersistentUID, DevicePath, DateTaken, LastWriteTime, Downloaded) 
                                        VALUES ($hash, $persistentUID, $devicePath, $dateTaken, $lastWriteTime, $downloaded)";
                    cmd.Parameters.AddWithValue("$hash", fileInfo.Hash);
                    cmd.Parameters.AddWithValue("$persistentUID", fileInfo.PersistentUID);
                    cmd.Parameters.AddWithValue("$devicePath", fileInfo.DevicePath);
                    cmd.Parameters.AddWithValue("$dateTaken", fileInfo.DateTaken.ToString());
                    cmd.Parameters.AddWithValue("$lastWriteTime", fileInfo.LastWriteTime.ToString());
                    cmd.Parameters.AddWithValue("$downloaded", Convert.ToInt32(fileInfo.Downloaded));
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }

    public class DBFileInfo
    {
        public string Hash { get; set; } = "";
        public string PersistentUID { get; set; } = "";
        public string DevicePath { get; set; } = "";
        public DateTime DateTaken { get; set; }
        public DateTime LastWriteTime { get; set; }
        public bool Downloaded { get; set; }
    }

    public class DBDeviceInfo
    {
        public string OrigName { get; set; } = "";
        public string CustomName { get; set; } = "";
        public string CustomType { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string SerialNum { get; set; } = "";
        public DateTime LastBackup { get; set; }
    }
}
