using System;
using System.IO;
using SQLite;
using Foundation;

namespace Chatter.iOS.Database
{
    public static class ChatterDatabase
    {
        public static void CreateDatabase()
        {
            var databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Chatter.db");

            var db = new SQLiteConnection(databasePath);
            db.CreateTable<ChatUserModel>();

            AddChatUsers(db, string.Empty);

            NSUserDefaults.StandardUserDefaults.SetBool(true, "DBCreated");
            NSUserDefaults.StandardUserDefaults.Synchronize();
        }

        public static string DatabasePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Chatter.db"); ;
        }

        public static void AddChatUsers(SQLiteConnection db, string symbol)
        {
            var s = db.Insert(new ChatUserModel()
            {
                Id = "BADB229",
                DisplayName = "Chatter",
                Text = string.Empty,
                ChatDate = DateTime.MinValue,
                PhoneNumber = "9028851589"
            });
        }
    }

    public class ChatUserModel
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Text { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime ChatDate { get; set; }
    }
}
