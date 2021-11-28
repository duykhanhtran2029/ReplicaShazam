using Shazam.AudioFormats;
using Shazam.AudioProcessing;
using Shazam.AudioProcessing.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("ShazamUnitTests")]
namespace Shazam
{
    public partial class Shazam
	{
		public Shazam()
		{
			Action loadFingerPrints = () => LoadFingerprints();
			Action loadMetadata = () => LoadMetaData();
			Parallel.Invoke(loadFingerPrints, loadMetadata);
		
		}


		/// <summary>
		/// Bits are stored as BE
		/// <para>Key:</para>
		/// <para> 9 bits =  frequency of anchor</para>
		/// <para> 9 bits =  frequency of point</para>
		/// <para> 14 bits =  delta</para>
		/// <para>Value (List of):</para>
		/// <para>32 bits absolute time of anchor</para>
		/// <para>32 bits id of a song</para>
		/// </summary>
		private Dictionary<uint, List<ulong>>[] databases;
		/// <summary>
		/// Currently used highest songID
		/// </summary>
		private uint maxSongID;
		/// <summary>
		/// <para>Key: songID</para>
		/// <para>Value: Song with metadata</para>
		/// </summary>
		public static List<Song> metadata;


		/// <summary>
		/// <para>Add a new song to the database.</para>
		/// <para>WARNING: Song must be sampled at 48000Hz!</para>
		/// </summary>
		/// <param name="path">Location of .wav audio file</param>
		public void AddNewSong(string path)
		{
			string name = path.Substring(path.Length - 4 - 8, path.Length - 4);
			List<TimeFrequencyPoint> TimeFrequencyPoitns = Processing(path);

			++maxSongID;
			//Create file with TFPs async
			Thread TFPSaver = new Thread(() =>
			{
				SaveTFPs(TimeFrequencyPoitns, maxSongID);
			});
			TFPSaver.Start();

			//Save Metadata async
			Thread MetadataSaver = new Thread(() =>
			{
				Song newSong = new Song(maxSongID, name);
				metadata.Add(newSong);
				DatabaseConnection.SaveSong(newSong);
				//SaveMetadata();
			});
			MetadataSaver.Start();

			//Add TFPs to database
			//AddTFPToDatabase(TimeFrequencyPoitns, maxSongID);
			Console.WriteLine("Add song successfully");
		}

		/// <summary>
		/// <para>Audio processing.</para>
		/// <para>WARNING: Song must be sampled at 48000Hz!</para>
		/// </summary>
		/// <param name="path">Location of .wav audio file</param>
		public List<TimeFrequencyPoint> Processing(string path, bool isFile = false)
        {
			//Plan of audio processing
			//STEREO -> MONO -> LOW PASS -> DOWNSAMPLE -> HAMMING -> FFT

			#region STEREO

			var audio = AudioReader.GetSound("../../../Resources/Songs/" + (isFile ? "Mix/" : "Wav/") + path);

			#endregion

			#region MONO

			if (audio.Channels == 2)  //MONO
				AudioProcessor.StereoToMono(audio);

			#endregion

			#region Short to Double

			double[] data = ShortArrayToDoubleArray(audio.Data);
			#endregion

			#region LOW PASS & DOWNSAMPLE

			var downsampledData = AudioProcessor.DownSample(data, Constants.DownSampleCoef, audio.SampleRate); //LOWPASS + DOWNSAMPLE
			data = null; //release memory
			#endregion

			#region HAMMING & FFT
			//apply FFT at every 1024 samples
			//get 512 bins 
			//of frequencies 0 - 6 kHZ
			//bin size of ~ 11,7 Hz

			int bufferSize = Constants.WindowSize / Constants.DownSampleCoef; //default: 4096/4 = 1024
			return CreateTimeFrequencyPoints(bufferSize, downsampledData, sensitivity: 1);
			#endregion
		}


		/// <summary>
		/// Records 10 sec of audio through microphone and finds best match in song database
		/// </summary>
		/// <returns></returns>
		public string RecognizeSong()
		{
			//recording of the song
			double[] data = RecordAudio(10000);
			
			//measure time of song searching
			Stopwatch stopwatch = Stopwatch.StartNew();
			stopwatch.Start();

			List<TimeFrequencyPoint> timeFrequencyPoints;
			#region Creating Time-frequency points
			int bufferSize = Constants.WindowSize / Constants.DownSampleCoef;
			timeFrequencyPoints= CreateTimeFrequencyPoints(bufferSize, data, sensitivity:0.9); //set higher sensitivity because microphone has lower sensitivity
			#endregion


			//find the best song in database
			Recogniser.FindBestMatch(databases, timeFrequencyPoints);

			stopwatch.Stop();
					
			return ($"   Song recognized in: {stopwatch.ElapsedMilliseconds} milliseconds");
		}

		/// <summary>
		/// Recognizing song from audio file
		/// <param name="path">Location of .wav audio file</param>
		/// </summary>
		/// <returns></returns>
		public string RecognizeFile(string path, bool isTest = false)
		{
			//measure time of song searching
			Stopwatch stopwatch = Stopwatch.StartNew();
			stopwatch.Start();

			List<TimeFrequencyPoint> timeFrequencyPoints = Processing(path, true);
			//find the best song in database
			string s = Recogniser.FindBestMatch(databases, timeFrequencyPoints);
			stopwatch.Stop();
			return isTest ? ($"{s}-{stopwatch.ElapsedMilliseconds}ms") : ($"   Song recognized in: {stopwatch.ElapsedMilliseconds} milliseconds");
		}

		/// <summary>
		/// Lists all songs in the database
		/// </summary>
		/// <param name="output">TextWriter to write the songs into</param>
		public void ListSongs(TextWriter output)
		{
			foreach (var song in metadata)
			{
				output.WriteLine(song.ID + "\t" + song.Name);
			}
		}
	}
}
