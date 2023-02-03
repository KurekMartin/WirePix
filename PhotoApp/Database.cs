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
            if (!File.Exists(_databaseName))
            {
                SQLiteConnection.CreateFile(_databaseName);
            }
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Devices(Id INTEGER PRIMARY KEY, OrigName TEXT, CustomName TEXT, SerialNum TEXT, LastBackup TEXT)";
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static void Connect()
        {
            Create();
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
            if(customName == "")
            {
                customName = origName;
            }
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

        public static string DeviceGetCustomName(string origName, string serialNum)
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
                        if (reader.HasRows)
                        {
                            reader.Read();
                            return reader.GetFieldValue<string>(reader.GetOrdinal("CustomName"));
                        }
                        return string.Empty;
                    }
                }
            }
        }

        public static void DeviceEditCustomName(string origName, string serialNum, string newCustomName)
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
                    cmd.Parameters.AddWithValue("$customName", newCustomName);
                    cmd.Parameters.AddWithValue("$origName", origName);
                    cmd.Parameters.AddWithValue("$serialNum", serialNum);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    public class DBDeviceInfo
    {
        public int Id { get; set; }
        public string OrigName { get; set; }
        public string CustomName { get; set; }
        public string SerialNum { get; set; }
        public DateTime LastBackup { get; set; }
    }
}
