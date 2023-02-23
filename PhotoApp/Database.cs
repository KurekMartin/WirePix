using System;
using System.Data.SQLite;
using System.IO;

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
                                            Id INTEGER PRIMARY KEY,
                                            OrigName TEXT, 
                                            CustomName TEXT, 
                                            SerialNum TEXT, 
                                            LastBackup TEXT)";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Files(
                                            Id INTEGER PRIMARY KEY, 
                                            Hash TEXT, 
                                            DevicePath TEXT,
                                            DateTaken TEXT,
                                            LastWriteTime TEXT,
                                            PersistentUID TEXT)";
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

        public static bool DeviceExists(string origName, string serialNum)
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"SELECT * FROM Devices 
                                        WHERE 
                                            OrigName = ($origName) 
                                        AND SerialNum = ($serialNum)";
                    cmd.Parameters.AddWithValue("$origName", origName);
                    cmd.Parameters.AddWithValue("$serialNum", serialNum);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.HasRows;
                    }
                }
            }
        }

        public static void DeviceInsert(string origName, string serialNum, string customName = "", DateTime lastBackup = new DateTime())
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"INSERT INTO Devices(OrigName, CustomName, SerialNum, LastBackup) 
                                        VALUES ($origName, $customName, $serialNum, $lastBackup)";
                    cmd.Parameters.AddWithValue("$origName", origName);
                    cmd.Parameters.AddWithValue("$customName", customName);
                    cmd.Parameters.AddWithValue("$serialNum", serialNum);
                    cmd.Parameters.AddWithValue("$lastBackup", lastBackup.ToString());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static (string customName, DateTime lastBackup) DeviceGetCustomName(string origName, string serialNum)
        {
            string customName = "";
            DateTime lastBackup = new DateTime();
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"SELECT * FROM Devices 
                                        WHERE 
                                            OrigName = ($origName) 
                                        AND SerialNum = ($serialNum)";
                    cmd.Parameters.AddWithValue("$origName", origName);
                    cmd.Parameters.AddWithValue("$serialNum", serialNum);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            customName = reader.GetFieldValue<string>(reader.GetOrdinal("CustomName"));
                            lastBackup = DateTime.Parse(reader.GetFieldValue<string>(reader.GetOrdinal("LastBackup")));
                        }
                    }
                }
            }
            return (customName, lastBackup);
        }

        public static void DeviceSetCustomName(string origName, string serialNum, string customName)
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
                                        AND SerialNum = ($serialNum)";
                    cmd.Parameters.AddWithValue("$customName", customName);
                    cmd.Parameters.AddWithValue("$origName", origName);
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

        public static DBFileInfo GetFileInfo(string persistentUID)
        {
            DBFileInfo fileInfo = null;
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"SELECT * FROM Files
                                        WHERE
                                            PersistentUID = ($persistentUID)";
                    cmd.Parameters.AddWithValue("$persistentUID", persistentUID);
                    using(SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fileInfo = new DBFileInfo() { 
                                Hash = reader.GetFieldValue<string>(reader.GetOrdinal("Hash")),
                                DevicePath = reader.GetFieldValue<string>(reader.GetOrdinal("DevicePath")),
                                PersistentUID = reader.GetFieldValue<string>(reader.GetOrdinal("PersistentUID")),
                                DateTaken = DateTime.Parse(reader.GetFieldValue<string>(reader.GetOrdinal("DateTaken"))),
                                LastWriteTime = DateTime.Parse(reader.GetFieldValue<string>(reader.GetOrdinal("LastWriteTime"))),
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
                    cmd.CommandText = @"INSERT INTO Files(Hash, PersistentUID, DevicePath, DateTaken, LastWriteTime) 
                                        VALUES ($hash, $persistentUID, $devicePath, $dateTaken, $lastWriteTime)";
                    cmd.Parameters.AddWithValue("$hash", fileInfo.Hash);
                    cmd.Parameters.AddWithValue("$persistentUID", fileInfo.PersistentUID);
                    cmd.Parameters.AddWithValue("$devicePath", fileInfo.DevicePath);
                    cmd.Parameters.AddWithValue("$dateTaken", fileInfo.DateTaken.ToString());
                    cmd.Parameters.AddWithValue("$lastWriteTime", fileInfo.LastWriteTime.ToString());
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }

    public class DBFileInfo
    {
        public string Hash { get; set; }
        public string PersistentUID { get; set; }
        public string DevicePath { get; set; }
        public DateTime DateTaken { get; set; }
        public DateTime LastWriteTime { get; set; }
    }
}
