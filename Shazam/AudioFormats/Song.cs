using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Runtime.Serialization;
using MongoDB.Bson.Serialization.Attributes;

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

	}
}
