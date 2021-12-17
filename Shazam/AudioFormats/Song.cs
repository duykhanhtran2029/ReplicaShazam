using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Runtime.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System.IO;
using Newtonsoft.Json;
using Shazam.Database;

namespace Shazam.AudioFormats
{
	public class Song
	{
		/// <summary>
		/// Instance of a song
		/// </summary>
		/// <param name="id">song ID</param>
		/// <param name="name">Name of the song</param>
		public Song(uint id, string name)
		{
			ID = id;
			Name = name;
		}

		[BsonId]
		[DataMember]
		public MongoDB.Bson.ObjectId _id { get; set; }
		                                                            

		[DataMember]
		[BsonElement("ID")]
		public uint ID { get; set; }



		[DataMember]
		[BsonElement("Name")]
		public string Name { get; set; }


		[DataMember]
		[BsonElement("Title")]
		public string Title { get; set; }

		[DataMember]
		[BsonElement("Artist")]
		public string Artist { get; set; }

		[DataMember]
		[BsonElement("LinkZingMp3")]
		public string LinkZingMp3 { get; set; }


		[DataMember]
		[BsonElement("LinkMV")]
		public string LinkMV { get; set; }

		[DataMember]
		[BsonElement("Link")]
		public string Link { get; set; }


		[DataMember]
		[BsonElement("Thumbnail")]
		public string Thumbnail { get; set; }



		public void UpdateMetaData(string title, string artist,string linkZingMp3,
			string link,string linkMV, string thumbnail)
        {
			this.Title = title;
			this.Artist = artist;
			this.LinkZingMp3 = linkZingMp3;
			this.Link = link;
			this.LinkMV = linkMV;
			this.Thumbnail = thumbnail;
        }


		public void ReadMetaData()
        {
			string metaPath = "../../../Resources/song/" + Name + ".txt";

			if(File.Exists(metaPath))
            {
				string metaData = File.ReadAllText(metaPath);
				MetaData data = JsonConvert.DeserializeObject<MetaData>(metaData);
				UpdateMetaData(data.title, data.artistsNames, data.link, ""
								, data.mvLink, data.thumbnailM);		

			}

		}

	}
}
