﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Shazam.AudioFormats;
using MongoDB.Driver;
using MongoDB.Bson;
using Shazam.Database;
using System.Threading;
using System.Linq;

namespace Shazam
{
	public partial class Shazam
	{
		/// <summary>
		/// Loads all fingerprints stored at <c>folderPath</c>
		/// </summary>
		/// <param name="folderPath">Folder with fingerprints</param>
		private void LoadFingerprints()
		{
			databases = new Dictionary<uint, List<ulong>>[6];
			for(int i = 0; i < 6; i ++)
				databases[i] = new Dictionary<uint, List<ulong>>();
			List<Thread> threads = new List<Thread>();

			//Get List Fingerprint
			IMongoCollection<Fingerprint> fingerPrintCollection = DatabaseConnection.GetFingerprintCollection();
			List <Fingerprint> fingerprints= fingerPrintCollection.Find(new BsonDocument()).ToList();
			
			maxSongID = (uint)fingerprints.Count;
			var splitFingerprints = fingerprints.Select((s, i) => new { s, i })
										.GroupBy(x => x.i % 6)
										.Select(g => g.Select(x => x.s).ToList())
										.ToList();
			// Start threads
			for (int i = 0; i < 6; i++)
			{
				int tmp = i; // Copy value for closure
				Thread t = new Thread(() => {
					foreach (Fingerprint fp in splitFingerprints[i])
						LoadSongFingerprint(fp, databases[i]);
					Console.WriteLine($"Database {i} was loaded");
				});
				t.Start();
				threads.Add(t);
			}

			// Join threads (wait threads)
			foreach (Thread thread in threads)
			{
				thread.Join();
			}
		}
		/// <summary>
		/// Loads fingerprint of song at <c>fingerprintPath</c> as song with <c>songID</c> ID.
		/// </summary>
		/// <param name="fingerprintPath">Location of the fingerprint.</param>
		/// <param name="songID">Song ID</param>
		private void LoadSongFingerprint(string fingerprintPath, uint songID, Dictionary<uint, List<ulong>> database)
		{
			List<TimeFrequencyPoint> timeFrequencyPoints = LoadTFP(fingerprintPath);
			AddTFPToDatabase(timeFrequencyPoints, songID, ref database);
		}

		/// <summary>
		/// Save fingerprint at <c>database</c>
		/// </summary>
		/// <param name="fingerprint"></param>
		private void LoadSongFingerprint(Fingerprint fingerprint, Dictionary<uint, List<ulong>> database)
		{
			List<TimeFrequencyPoint> timeFrequencyPoints = fingerprint.FTP;
			AddTFPToDatabase(timeFrequencyPoints, fingerprint.songID , ref database);
		}


		/// <summary>
		/// Loads Time-Frequency Points of a fingerprint at <c>fingerprintPath</c>
		/// </summary>
		/// <param name="fingerprintPath"></param>
		/// <returns>List of tuples of time, frequency</returns>
		private List<TimeFrequencyPoint> LoadTFP(string fingerprintPath = Constants.FingerprintPath)
		{
			//TFPs = Time-Frequency Points
			string tfps = File.ReadAllText(fingerprintPath);
			List<TimeFrequencyPoint> TFPs = JsonConvert.DeserializeObject<List<TimeFrequencyPoint>>(tfps);
			return TFPs;
		}
		/// <summary>
		/// Loads metadata to of songs from <c>metadataPath</c>
		/// </summary>
		/// <param name="metadataPath"/>
		private void LoadMetadata(string metadataPath = Constants.MetadataPath)
		{
			string metadatas = File.ReadAllText(metadataPath);
			if (!String.IsNullOrEmpty(metadatas))
				metadata = JsonConvert.DeserializeObject<List<Song>>(metadatas);
			else
				metadata = new List<Song>();
		}


		private void LoadMetaData()
		{
			IMongoCollection<Song> songCollection = DatabaseConnection.GetSongCollection();
            try
            {
				var list = songCollection.Find(new BsonDocument()).ToList();

				if (list != null)
				{
					metadata = list;
				}
				else
					metadata = new List<Song>();
			}
			catch (Exception e)
            {
				Console.WriteLine(e);
			}
			
		}
		/// <summary>
		/// Populates local database with TFPs
		/// </summary>
		/// <param name="timeFrequencyPoints">Time-frequency points of the song</param>
		/// <param name="songId">songID</param>
		private void AddTFPToDatabase(List<TimeFrequencyPoint> timeFrequencyPoints, in uint songId, ref Dictionary<uint, List<ulong>> database)
		{
			/* spectogram:
			 *
			 * |
			 * |       X X
			 * |         X
			 * |     X     X
			 * |   X         X
			 * | X X X     X
			 * x----------------
			 */


			// -targetZoneSize: because of end limit 
			// -1: because of anchor point at -2 position target zone
			int stopIdx = timeFrequencyPoints.Count - Constants.TargetZoneSize - Constants.AnchorOffset;
			for (int i = 0; i < stopIdx; i++)
			{
				//anchor is at idx i
				//1st in TZ is at idx i+3
				//5th in TZ is at idx i+7

				uint anchorFreq = timeFrequencyPoints[i].Frequency;
				uint anchorTime = timeFrequencyPoints[i].Time;
				ulong SongValue = BuildSongValue(anchorTime, songId);
				for (int pointNum = 3; pointNum < Constants.TargetZoneSize + 3; pointNum++)
				{
					uint pointFreq = timeFrequencyPoints[i + pointNum].Frequency;
					uint pointTime = timeFrequencyPoints[i + pointNum].Time;

					uint address = BuildAddress(anchorFreq, pointFreq, pointTime - anchorTime);

					if (!database.ContainsKey(address)) //create new instance if it doesnt exist
					{
						database.Add(address, new List<ulong>() { SongValue });
					}
					else //add SongValue to the list of
					{
						database[address].Add(SongValue);
					}
				}

			}
		}

	}
}
