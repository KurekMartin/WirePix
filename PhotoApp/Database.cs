using System.Data.SQLite;
using System.IO;

namespace PhotoApp
{
    public static class Database
    {
        private static string _dataFolder = App.Current.Resources[Properties.Keys.DataFolder].ToString();
        private static string _databaseName = Path.Combine(_dataFolder, "db.sqlite");
        private static SQLiteConnection _connection;
        public static void Create()
        {
            if (!File.Exists(_databaseName))
            {
                SQLiteConnection.CreateFile(_databaseName);
            }
            _connection = new SQLiteConnection(@"URI=file:"+_databaseName);
            _connection.Open();
            SQLiteCommand cmd = new SQLiteCommand(_connection);
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS devices(id INTEGER PRIMARY KEY, orig_name TEXT, custom_name TEXT, serial_num TEXT, last_backup TEXT)";
            cmd.ExecuteNonQuery();
            _connection.Close();
        }
        public static void Connect()
        {
            Create();
        }

        public static SQLiteConnection Connection { get { return _connection; } }
    }
}
