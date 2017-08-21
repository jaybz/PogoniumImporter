using SQLite.Net.Platform.XamarinAndroid;
using PogoniumImporter.PokemonDatabase;
using SQLite.Net.Async;
using System;
using System.Threading.Tasks;

namespace PogoniumImporter.Droid
{
    class DatabaseHelper
    {
        private static SQLiteAsyncConnection db = null;

        public static async Task Initialize()
        {
            Database.SetPlatform(new SQLitePlatformAndroidN());
            Database.SetPath(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            await Database.Initialize();
            DatabaseHelper.db = Database.GetConnection();
        }

        public static SQLiteAsyncConnection GetConnection()
        {
            return db;
        }

        public static async Task RefreshData(IProgress<int> progress = null)
        {
            await Database.RefreshData(progress);
        }
    }
}