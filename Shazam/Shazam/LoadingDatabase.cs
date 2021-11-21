using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Shazam.AudioFormats;

namespace Shazam
{
	public partial class Shazam
	{
		/// <summary>
		/// Loads all fingerprints stored at <c>folderPath</c>
		/// </summary>
		/// <param name="folderPath">Folder with fingerprints</param>
		private void LoadFingerprints(string folderPath)
		{
			databases = new Dictionary<uint, List<ulong>>[6];
			for(int i = 0; i < 6; i ++)
				databases[i] = new Dictionary<uint, List<ulong>>();
			
			foreach (string file in Directory.EnumerateFiles(folderPath, "*.json"))
			{
				Regex rx = new Regex(@"\\(?<songID>\d+).json"); //regex for matching songID

				if (uint.TryParse(rx.Match(file).Groups["songID"].Value, out uint songID))
				{
					Dictionary<uint, List<ulong>> tmp = new Dictionary<uint, List<ulong>>();
					if (songID <= 100)
                    {
						LoadSongFingerprint(file, songID, databases[0]);
					}
					if (songID > 100 && songID <= 200)
                    {
						LoadSongFingerprint(file, songID, databases[1]);
					}
					if (songID > 200 && songID <= 300)
					{
						LoadSongFingerprint(file, songID, databases[2]);
					}
					if (songID > 300 && songID <= 400)
					{
						LoadSongFingerprint(file, songID, databases[3]);
					}
					if (songID > 400 && songID <= 500)
					{
						LoadSongFingerprint(file, songID, databases[4]);
					}
					if (songID > 500 && songID <= 600)
					{
						LoadSongFingerprint(file, songID, databases[5]);
					}
					Console.WriteLine($"   Song ID: {songID} was loaded.");
					maxSongID = Math.Max(maxSongID, songID);
				}
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
