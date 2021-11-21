using Shazam.AudioFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shazam
{
	public partial class Shazam
	{
		public class Recogniser
		{
			/// <summary>
			/// Finds song from database that corresponds the best to the recording.
			/// </summary>
			/// <param name="database"></param>
			/// <param name="recordAddresses"></param>
			/// <returns></returns>
			public static string FindBestMatch(Dictionary<uint, List<ulong>> [] databases, List<TimeFrequencyPoint> timeFrequencyPoints)
			{
				//[address;(AbsAnchorTimes)]
				Dictionary<uint, List<uint>> recordAddresses = CreateRecordAddresses(timeFrequencyPoints);
				Dictionary<uint, int>[] deltas = new Dictionary<uint, int>[6];

				List<Thread> threads = new List<Thread>();

				// Start threads
				for (int i = 0; i < 6; i++)
				{
					int tmp = i; // Copy value for closure
					Thread t = new Thread(() => {
						var quantities = GetSongValQuantities(recordAddresses, databases[tmp]);
						var filteredSongs = FilterSongs(recordAddresses, quantities, databases[tmp]);
						deltas[tmp] = GetDeltas(filteredSongs, recordAddresses);
					});
					t.Start();
					threads.Add(t);
				}

				// Join threads (wait threads)
				foreach (Thread thread in threads)
				{
					thread.Join();
				}

				var result = new Dictionary<uint, int>();
				foreach (var dict in deltas)
					foreach (var x in dict)
						result[x.Key] = x.Value;
				//pick top songs with highest delta
				return MaximizeTimeCoherency(result, timeFrequencyPoints.Count);
			}

			/// <summary>
			/// Picks song with the most notes corresponding to the recording.
			/// </summary>
			/// <param name="deltas">[songID, num of time coherent notes]</param>
			/// <param name="totalNotes">total number of notes in recording</param>
			/// <returns></returns>
			private static string MaximizeTimeCoherency(Dictionary<uint, int> deltas, int totalNotes)
			{
				//Console.WriteLine($"   Total number of notes: {totalNotes}");
				var sortedDeltas = (from entry in deltas orderby entry.Value descending select entry).Take(5);
				foreach(var e in sortedDeltas)
                {
					Console.WriteLine($"{metadata[unchecked((int)e.Key) - 1].Name}: {(double)e.Value / totalNotes * 100:##.###}%");
                }

				var top = sortedDeltas.FirstOrDefault();
				double score = (double)sortedDeltas.FirstOrDefault().Value / totalNotes;
				if (score > 1)
					score = 1.00;
				if (sortedDeltas.Count() != 0)
					return $"{metadata[unchecked((int)top.Key)-1].Name}.wav-{score * 100:##.###}%";
				else
					return "";
			}

			/// <summary>
			/// Returns 
			/// </summary>
			/// <param name="filteredSongs"></param>
			/// <param name="recordAddresses"></param>
			/// <returns></returns>
			private static Dictionary<uint, int> GetDeltas(Dictionary<uint, Dictionary<uint, List<uint>>> filteredSongs, Dictionary<uint, List<uint>> recordAddresses)
			{
				//[songID, delta]
				Dictionary<uint, int> maxTimeCoherentNotes = new Dictionary<uint, int>();
				foreach (var songID in filteredSongs.Keys)
				{
					//[delta, occurrence]
					Dictionary<long, int> songDeltasQty = new Dictionary<long, int>();
					foreach (var address in recordAddresses.Keys)
					{
						if (filteredSongs[songID].ContainsKey(address))
						{
							/*
							 * DEBUG NOTE: at 400Hz song, there are 5 different addresses
							 * 			- because: each TGZ has 5 samples so 5 offsets must be created
							 */
							foreach (var absSongAnchTime in filteredSongs[songID][address]) //foreach AbsSongAnchorTime at specific address 
							{
								foreach (var absRecAnchTime in recordAddresses[address]) //foreach AbsRecordAnchorTime at specific address
								{
									//delta can be negative (RecTime = 8 with mapped SongTime = 2)
									long delta = (long)absSongAnchTime - (long)absRecAnchTime;
									if (!songDeltasQty.ContainsKey(delta))
										songDeltasQty.Add(delta, 0);
									songDeltasQty[delta]++;
								}
							}
						}
					}
					//get number of notes that are coherent with the most deltas (each note has same delta from start of the song)
					int timeCohNotes = MaximizeDelta(songDeltasQty);
					maxTimeCoherentNotes.Add(songID, timeCohNotes);
				}
				return maxTimeCoherentNotes;
			}

			/// <summary>
			/// Gets the delta with the most occurrences
			/// </summary>
			/// <param name="deltasQty">[delta, quantity]</param>
			/// <returns></returns>
			private static int MaximizeDelta(Dictionary<long, int> deltasQty)
			{
				int maxOccrence = 0;
				foreach (var pair in deltasQty)
				{
					if (pair.Value > maxOccrence)
					{
						maxOccrence = pair.Value;
					}
				}
				return maxOccrence;
			}

			/// <summary>
			/// Gets quantities of song values connected with common addresses in recording.
			/// </summary>
			/// <param name="recordAddresses">addresses in recording</param>
			/// <returns>[songValue, occurrence]</returns>
			private static Dictionary<ulong, int> GetSongValQuantities(Dictionary<uint, List<uint>> recordAddresses, Dictionary<uint, List<ulong>> database)
			{
				var quantities = new Dictionary<ulong, int>();

				foreach (var address in recordAddresses.Keys)
				{
					if (database.ContainsKey(address))
					{
						foreach (var songValue in database[address])
						{
							if (!quantities.ContainsKey(songValue))
							{
								quantities.Add(songValue, 1);
							}
							else
							{
								quantities[songValue]++;

							}
						}
					}
				}

				return quantities;
			}

			/// <summary>
			/// Filter out songs that don't have enough common samples with recording
			/// </summary>
			/// <param name="recordAddresses">Addresses in recording</param>
			/// <param name="quantities">occurrences of songvalues common with recording</param>
			/// <returns>[songID, [address, (absSongAnchorTime)]]</returns>
			private static Dictionary<uint, Dictionary<uint, List<uint>>> FilterSongs(Dictionary<uint, List<uint>> recordAddresses, Dictionary<ulong, int> quantities, Dictionary<uint, List<ulong>> database)
			{
				//[songID, [address, (absSongAnchorTime)]]
				Dictionary<uint, Dictionary<uint, List<uint>>> res = new Dictionary<uint, Dictionary<uint, List<uint>>>();
				//[songID, common couple amount]
				Dictionary<uint, int> commonCoupleAmount = new Dictionary<uint, int>();
				//[songID, common couples in TGZ amount]
				Dictionary<uint, int> commonTGZAmount = new Dictionary<uint, int>();

				//Create datastructure for fast search in time coherency check
				//SongID -> Address -> absSongAnchorTime
				foreach (var address in recordAddresses.Keys)
				{
					if (database.ContainsKey(address))
					{
						foreach (var songValue in database[address])
						{
							uint songID = (uint)songValue;
							if (!commonCoupleAmount.ContainsKey(songID))
								commonCoupleAmount.Add(songID, 0);
							commonCoupleAmount[songID]++;

							//filter out addresses that do not create TGZ
							if (quantities[songValue] >= 5)
							{
								if (!commonTGZAmount.ContainsKey(songID))
									commonTGZAmount.Add(songID, 0);
								commonTGZAmount[songID]++;


								uint AbsSongAnchTime = (uint)(songValue >> 32);

								if (!res.ContainsKey(songID)) //add songID entry
									res.Add(songID, new Dictionary<uint, List<uint>>());
								if (!res[songID].ContainsKey(address)) //add address entry
									res[songID].Add(address, new List<uint>());

								//add the actual Absolute Anchor Time of a song
								res[songID][address].Add(AbsSongAnchTime);
							}
						}
					}
				}

				//Print number of common couples in each song and record

				//foreach (uint songID in res.Keys)
				//{
				//	int couples = commonCoupleAmount[songID];
				//	int TGZs = commonTGZAmount[songID];
				//	//NOTE: result can be more than 100% (some parts of the songs may repeat - refrains)
				//	//Console.WriteLine($"   Song ID: {songID} has {couples} couples where {TGZs} are in target zones: {(double)TGZs / couples * 100:##.###} %");
					
				//}

                //remove songs that have low ratio of couples that make a TGZ
                //also remove songs that have low amount of samples common with recording

                foreach (var songID in res.Keys)
                {
                    double ratio = (double)commonTGZAmount[songID] / commonCoupleAmount[songID];
                    //remove song if less than half of samples is not in TGZ
                    //remove songs that don't have enough samples in TGZ
                    //		- min is 1720 * coef (for noise cancellation)
                    //			- avg 2. samples per bin common (2 out of 6) with about 860 bins per 10 (1000/11.7) seconds = 1720

                    if ((commonTGZAmount[songID] < 1400 || ratio < Constants.SamplesInTgzCoef) && //normal songs have a lot of samples in TGZ with not that high ratio (thus wont get deleted)
                        ratio < 0.8d) //or test sounds (Hertz.wav or 400Hz.wav) dont have many samples in TGZ but have high ratio
                    {
                        res.Remove(songID);
                    }
                }

                return res;
			}

			/// <summary>
			/// Creates Address to Absolute time anchor dictionary out of TFPs
			/// </summary>
			/// <param name="timeFrequencyPoints"></param>
			/// <returns></returns>
			private static Dictionary<uint, List<uint>> CreateRecordAddresses(List<TimeFrequencyPoint> timeFrequencyPoints)
			{
				Dictionary<uint, List<uint>> res = new Dictionary<uint, List<uint>>();


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
					for (int pointNum = 3; pointNum < Constants.TargetZoneSize + 3; pointNum++)
					{
						uint pointFreq = timeFrequencyPoints[i + pointNum].Frequency;
						uint pointTime = timeFrequencyPoints[i + pointNum].Time;

						uint address = BuildAddress(anchorFreq, pointFreq, pointTime - anchorTime);

						if (!res.ContainsKey(address)) //create new instance if it doesnt exist
							res.Add(address, new List<uint>());
						res[address].Add(anchorTime); //add Anchor time to the list
					}
				}
				return res;
			}

		}
	}

}
