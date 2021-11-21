using Shazam.AudioFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shazam
{
	public partial class Shazam
	{
		/// <summary>
		/// Saves TFPs as as .txt file
		/// </summary>
		/// <param name="timeFrequencyPoitns">TFPs</param>
		/// <param name="songID">ID to associate TFPs with</param>
		private void SaveTFPs(List<TimeFrequencyPoint> timeFrequencyPoitns, in uint songID)
		{
			string json = JsonSerializer.Serialize(timeFrequencyPoitns);
			File.WriteAllText($"Resources/Fingerprints/{songID}.json", json);
		}
		/// <summary>
		/// Saves song metadata.
		/// </summary>
		/// <param name="metadataPath"></param>
		private void SaveMetadata(string metadataPath = Constants.MetadataPath)
		{
			string json = JsonSerializer.Serialize(metadata);
			File.WriteAllText(metadataPath, json);
		}

	}
}
