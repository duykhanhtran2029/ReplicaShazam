using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Driver;
using MongoDB.Bson;
using System.IO;
using Shazam.AudioFormats;
using Shazam.Database;
using dotenv.net;
using dotenv.net.Utilities;

namespace Shazam
{

    class DatabaseConnection
    {
        private static MongoClient client = null;
        private static IMongoCollection<Fingerprint> fingerPrintCollection = null;
        private static IMongoCollection<Song> songCollection = null;
        public static string DBName = null;
        public static string DBPath = null;
        public static string ftpColName = null;
        public static string songColName = null;
        private DatabaseConnection()
        {
            TextWriter output = Console.Out;
            try
            {
                client = new MongoClient(DBPath);
                output.WriteLine("Connect to MongoDB successfully at port 27017");
            }
            catch
            {
                output.WriteLine("Meet Error when connecting to MongoDB");
            }
        }
        public static MongoClient GetConnection()
        {
            if (client == null)
            {
                client = new MongoClient(DBPath);
            }
            return client;
        }
        public static IMongoCollection<Fingerprint> GetFingerprintCollection()
        {
            return fingerPrintCollection;
        }
        public static IMongoCollection<Song> GetSongCollection()
        {
            return songCollection;
        }
        public static void CreateConnection()
        {

            if (client == null)
            {

                // Read parameter from .env file at bin/debug
                DotEnv.Load();
                var env = DotEnv.Read();
                DBName = EnvReader.GetStringValue("DB_NAME");
                ftpColName = env["FTP_COLLECTION"];
                DBPath = env["MONGO_PATH"];
                songColName = env["SONG_COLLECTION"];


                TextWriter output = Console.Out;
                try
                {

                  /*  var settings = MongoClientSettings.FromConnectionString("mongodb+srv://huutri148:tri12345678@cluster0.vs3cl.mongodb.net/shazam?retryWrites=true&w=majority");
                    client = new MongoClient(settings);*/
                    client = new MongoClient(DBPath);
                    var db = client.GetDatabase(DBName);
                    fingerPrintCollection = db.GetCollection<Fingerprint>(ftpColName);
                    songCollection = db.GetCollection<Song>(songColName);


                    output.WriteLine("Connect to MongoDB successfully at port 27017");
                }
                catch
                {
                    output.WriteLine("Meet Error when connecting to MongoDB");
                }
            }

            /* var settings = MongoClientSettings.FromConnectionString("mongodb+srv://huutri148:tri12345678@cluster0.vs3cl.mongodb.net/shazam?retryWrites=true&w=majority");
             client = new MongoClient(settings);
             var db = client.GetDatabase("shazam");
             var col = db.ListCollectionNames().ToList();

             foreach(var c in col)
             {
                 Console.WriteLine(c);
             }*/






        }
        public static void SaveFingerPrint(Fingerprint ftp)
        {
            fingerPrintCollection.InsertOne(ftp);
        }


        public static void SaveSong(Song song)
        {
            songCollection.InsertOne(song);
        }


        public void SetupDB ()
        {
            DotEnv.Load();
            var env = DotEnv.Read();
            DBName = env["DB_NAME"];
            ftpColName = env["FTP_COLLECTION"];
            songColName = env["SONG_COLLECTION "];
        }
    }
}
