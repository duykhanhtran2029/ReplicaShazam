using Shazam.AudioFormats;
using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Runtime.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Shazam.Database
{
    class Fingerprint
    {
        [BsonId]
        [DataMember]
        public MongoDB.Bson.ObjectId _id { get; set; }


        [DataMember]
        [BsonElement("FTP")]
        public List<TimeFrequencyPoint> FTP;


        [DataMember]
        [BsonElement("songID")]
        public uint songID;

        public Fingerprint(in uint _songID, List<TimeFrequencyPoint> _FTP)
        {
            FTP = _FTP;
            songID = _songID;
        }

    }
}
