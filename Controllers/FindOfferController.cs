using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model;
using System.Threading;
using DataAccessLayer;
using System.Diagnostics;
using System.IO;

namespace Controllers
{
	public class FindOfferController : MainController
	{
		private static HashSet<RefHashSet<Shop>> uniqueCombinations;
		private static CompleteOffer cheapestCompleteOffer;
		private static float cheapestCompleteOfferPrice;
		private static int combinationsChecked;
		private static StringBuilder displayUpdate;
		private static float shippingCosts;
		private static float currencyModifier;
		private static int maxDeepth;
		private static int maxShops;
		private static float maxMedianPercentage;
		private static bool isMaxMedianPercentageEnabled;

		public static void AsyncFindOffer(
			float currencyMod, 
			float shipping, 
			int maximumShops, 
			int maximumDeepth, 
			float maxMedianPercentage2, 
			bool isMaxMedianPercentageEnabled2 )
		{
			//Initiate
			actionIsCancelled = false;
			uniqueCombinations = new HashSet<RefHashSet<Shop>>();
			cheapestCompleteOffer = null;
			cheapestCompleteOfferPrice = float.MaxValue;
			displayUpdate = new StringBuilder();
			shippingCosts = shipping;
			currencyModifier = currencyMod;
			maxDeepth = maximumDeepth;
			maxShops = maximumShops;
			maxMedianPercentage = maxMedianPercentage2;
			isMaxMedianPercentageEnabled = isMaxMedianPercentageEnabled2;

			Thread t1 = new Thread(RunFindOfferStrategies);
			t1.IsBackground = true;
			t1.Start();
		}

		private static Stopwatch TimerTotal = new Stopwatch();
		private static Stopwatch TimerBlacklist = new Stopwatch();
		private static Stopwatch TimerPriceIndex = new Stopwatch();
		private static Stopwatch TimerMostLotsStrategy = new Stopwatch();
		private static Stopwatch TimerRarestLotStrategy = new Stopwatch();
		private static Stopwatch TimerCalculatingCombinations = new Stopwatch();

		private static void RunFindOfferStrategies()
		{
			//Set up priceindex and other things
			TimerTotal.Restart();

			displayUpdate.Append("Applying blacklist...");
			OverwriteDisplay(displayUpdate.ToString());

			TimerBlacklist.Restart();
			ApplyBlacklist();
			TimerBlacklist.Stop();

			displayUpdate.Append("done in " + TimerBlacklist.Elapsed.ToString(@"hh\:mm\:ss") + ".\n");
			OverwriteDisplay(displayUpdate.ToString());


			displayUpdate.Append("Calculating price index...");
			OverwriteDisplay(displayUpdate.ToString());

			TimerPriceIndex.Restart();
			InitiateRankedShopAndPriceIndex();
			TimerPriceIndex.Stop();

			displayUpdate.Append("done in " + TimerPriceIndex.Elapsed.ToString(@"hh\:mm\:ss") + ".\n");
			OverwriteDisplay(displayUpdate.ToString());


			uniqueCombinations.Clear();
			cheapestCompleteOfferPrice = float.MaxValue;
			combinationsChecked = 0;

			displayUpdate.Append("Finding combinations by most lots: ");
			OverwriteDisplay(displayUpdate.ToString());

			TimerMostLotsStrategy.Restart();
			//RecursiveMostLotsStrategy(DAO.GetWantedList(), DAO.GetShops(), new HashSet<Shop>(), 0, 100);
			TimerMostLotsStrategy.Stop();

			displayUpdate.Append(uniqueCombinations.Count.ToString() +
				" unique combinations found, " + combinationsChecked.ToString() +
				" checked in " + TimerMostLotsStrategy.Elapsed.ToString(@"hh\:mm\:ss") + "\n");
			OverwriteDisplay(displayUpdate.ToString());

			displayUpdate.Append("Finding combinations by rarest lot: ");
			OverwriteDisplay(displayUpdate.ToString());

			int combinationsFoundUntilNow = uniqueCombinations.Count;
			int combinationsCheckedUntilNow = combinationsChecked;

			TimerRarestLotStrategy.Restart();
			RecursiveRarestLotStrategy(DAO.GetWantedList(), DAO.GetShops().Where(s => !s.IsBlacklisted).ToList(), new HashSet<Shop>(), 0, 100);
			TimerRarestLotStrategy.Stop();

			displayUpdate.Append((uniqueCombinations.Count - combinationsFoundUntilNow).ToString() + " additional combinations found, " +
				(combinationsChecked - combinationsCheckedUntilNow).ToString() + " additional combinations checked in " +
				TimerRarestLotStrategy.Elapsed.ToString(@"hh\:mm\:ss") + "\n");
			OverwriteDisplay(displayUpdate.ToString());

			displayUpdate.Append("Calculating combination ");

			TimerCalculatingCombinations.Restart();
			CalculateBestCombination();
			TimerCalculatingCombinations.Stop();
			TimerTotal.Stop();



			if (cheapestCompleteOffer != null)
			{
				displayUpdate.Append(uniqueCombinations.Count.ToString() + " of " + uniqueCombinations.Count.ToString() + ". Checked in " +
				TimerCalculatingCombinations.Elapsed.ToString(@"hh\:mm\:ss") + ".\n" +
				"Cheapest offer found: " + (cheapestCompleteOffer.Price * currencyModifier).ToString("0.00") + ".\n\n" +
				"Search completed in " + TimerTotal.Elapsed.ToString(@"hh\:mm\:ss") + ", results below.\n\n");

				OverwriteDisplay(displayUpdate.ToString());

				displayUpdate.Append(CheckForMissingBricks());

				displayUpdate.Append(cheapestCompleteOffer.ToString(shippingCosts, currencyModifier,true));
				OverwriteDisplay(displayUpdate.ToString());
			}
			else
			{
				displayUpdate.Append(uniqueCombinations.Count.ToString() + " of " + uniqueCombinations.Count.ToString() + ". Checked in " +
				TimerCalculatingCombinations.Elapsed.ToString(@"hh\:mm\:ss") + ".\n" +
				"No offer found.\n\n" +
				"Search completed in " + TimerTotal.Elapsed.ToString(@"hh\:mm\:ss") + ".");

				OverwriteDisplay(displayUpdate.ToString());
			}

			UnlockUI();
		}

		private static void ApplyBlacklist()
		{
			List<string> shopBlacklist = new List<string>();
			List<string> wordBlacklist = new List<string>();

			using (StreamReader sr = new StreamReader("ShopBlacklist.txt"))
			{
				string[] entries = sr.ReadToEnd().Split('#');

				for (int i = 0; i < entries.Length; i++)
				{
					if (!entries[i].Equals(""))
					{
						shopBlacklist.Add(entries[i].Trim().ToLower());
					}
				}
			}

			using (StreamReader sr = new StreamReader("WordBlacklist.txt"))
			{
				string[] entries = sr.ReadToEnd().Split('#');

				for (int i = 0; i < entries.Length; i++)
				{
					if (!entries[i].Equals(""))
					{
						wordBlacklist.Add(entries[i].Trim().ToLower());
					}
				}
			}

			List<Shop> shops = DAO.GetShops();
			for (int i = 0; i < shops.Count; i++)
			{
				shops[i].UpdateBlacklistStatus(shopBlacklist, wordBlacklist);
			}
		}

		/// <summary>
		/// Simulate, check, unsimulate.
		/// </summary>
		private static string CheckForMissingBricks()
		{
			cheapestCompleteOffer.Simulate(true);

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < DAO.GetWantedList().allLots.Count; i++)
			{
				if (DAO.GetWantedList().allLots[i].WantedQuantity > 0)
				{
					sb.Append(DAO.GetWantedList().allLots[i].ToString() + "\n");
				}
			}

			if (sb.Length > 0)
			{
				sb.Insert(0, "The following bricks are missing:\n");
				sb.Append("\n");
			}

			cheapestCompleteOffer.Simulate(false);

			return sb.ToString();
		}

		#region PriceIndex

		private static void InitiateRankedShopAndPriceIndex()
		{
			WantedList wantedList = DAO.GetWantedList();
			List<Shop> shops = DAO.GetShops();

			//Initiate
			IDictionary<Brick, SortedDictionary<float, int>> newBrickPrices = new Dictionary<Brick, SortedDictionary<float, int>>();
			IDictionary<Brick, SortedDictionary<float, int>> usedBrickPrices = new Dictionary<Brick, SortedDictionary<float, int>>();

			IDictionary<Brick, int> newBrickAmounts = new Dictionary<Brick, int>();
			IDictionary<Brick, int> usedBrickAmounts = new Dictionary<Brick, int>();

			for (int i = 0; i < wantedList.allLots.Count; i++)
			{
				newBrickPrices[wantedList.allLots[i].Brick] = new SortedDictionary<float, int>();
				usedBrickPrices[wantedList.allLots[i].Brick] = new SortedDictionary<float, int>();

				newBrickAmounts[wantedList.allLots[i].Brick] = 0;
				usedBrickAmounts[wantedList.allLots[i].Brick] = 0;
			}

			//Record prices
			for (int j = 0; j < shops.Count; j++)
			{
				shops[j].AddBrickPrices(newBrickPrices, usedBrickPrices, newBrickAmounts, usedBrickAmounts);
			}

			//Create and inititate median price indexes
			IDictionary<Brick, float> newMedianBrickPrices = new Dictionary<Brick, float>();
			IDictionary<Brick, float> usedMedianBrickPrices = new Dictionary<Brick, float>();

			for (int l = 0; l < newBrickPrices.Count; l++)
			{
				if (newBrickPrices.ElementAt(l).Value.Count > 0)
				{
					newMedianBrickPrices[newBrickPrices.ElementAt(l).Key] =
						CalculateMedianBrickPrice(newBrickPrices.ElementAt(l).Value, newBrickAmounts[newBrickPrices.ElementAt(l).Key]);
				}

				if (usedBrickPrices.ElementAt(l).Value.Count > 0)
				{
					usedMedianBrickPrices[usedBrickPrices.ElementAt(l).Key] =
						CalculateMedianBrickPrice(usedBrickPrices.ElementAt(l).Value, usedBrickAmounts[usedBrickPrices.ElementAt(l).Key]);
				}
			}

			//Rank shops
			for (int i = 0; i < shops.Count; i++)
			{
				shops[i].CalculateShopRankI(newMedianBrickPrices, usedMedianBrickPrices, wantedList);
			}

			//Should be an independent method but I put it here because its not that important
			//Apply median price cutoff
			for (int i = 0; i < shops.Count; i++)
			{
				shops[i].SetMaxPercentageOverMedianPrice(isMaxMedianPercentageEnabled, maxMedianPercentage, newMedianBrickPrices, usedMedianBrickPrices);
			}

		}

		private static float CalculateMedianBrickPrice(SortedDictionary<float, int> prices, int totalAmount)
		{
			float median = -1;

			if (totalAmount == 1)
			{
				median = prices.ElementAt(0).Key;
			}
			else if (totalAmount % 2 == 0)
			{
				int amountSoFar = 0;
				int medianBrickOne = totalAmount / 2;

				for (int i = 0; i < prices.Count; i++)
				{
					amountSoFar += prices.Values.ElementAt(i);
					if (amountSoFar >= medianBrickOne)
					{
						if (prices.Values.ElementAt(i) == 1)
						{
							median = (prices.Keys.ElementAt(i) + prices.Keys.ElementAt(i + 1)) / 2;
							break;
						}
						else
						{
							median = prices.Keys.ElementAt(i);
							break;
						}
					}
				}
			}
			else
			{
				int amountSoFar = 0;
				int medianBrick = (totalAmount + 1) / 2;
				for (int i = 0; i < prices.Count; i++)
				{
					amountSoFar += prices.Values.ElementAt(i);
					if (amountSoFar >= medianBrick)
					{
						median = prices.Keys.ElementAt(i);
						break;
					}
				}
			}

			return median;
		}

		#endregion

		//---------------- Most Lots --------------//
		private static void RecursiveMostLotsStrategy(
			WantedList wantedList,
			List<Shop> shopCandidates,
			HashSet<Shop> selectedShops,
			float currentProgress,
			float currentPercentage)
		{

			List<Shop> topShops = GetTopMostLotsShops(wantedList, shopCandidates);

			if (topShops.Count > 0)
			{
				float newPercentage = currentPercentage / topShops.Count;

				for (int i = 0; i < topShops.Count(); i++)
				{
					topShops[i].SimulatePurchase(wantedList);
					shopCandidates.Remove(topShops[i]);
					selectedShops.Add(topShops[i]);

					float newProgress = currentProgress + newPercentage * i;

					RecursiveMostLotsStrategy(wantedList, shopCandidates, selectedShops, newProgress, newPercentage);

					topShops[i].CancelPurchase();
					shopCandidates.Add(topShops[i]);
					selectedShops.Remove(topShops[i]);
				}
			}
			else
			{
				if (!uniqueCombinations.Contains(selectedShops))
				{
					uniqueCombinations.Add(new RefHashSet<Shop>(selectedShops));
				}

				combinationsChecked++;
				if (combinationsChecked % 10 == 0)
				{
					OverwriteDisplay(displayUpdate.ToString() + currentProgress.ToString("00.00") + "%: " +
						uniqueCombinations.Count.ToString() + " unique combinations found, " + combinationsChecked.ToString() + " checked.");
					Thread.Sleep(0);
				}
			}
		}

		/// <summary>
		/// Returns ALL shops with maximum lots, or the 4 next best
		/// </summary>
		/// <param name="wantedList"></param>
		/// <param name="shopCandidates"></param>
		/// <returns></returns>
		private static List<Shop> GetTopMostLotsShops(WantedList wantedList, List<Shop> shopCandidates)
		{
			List<Shop> topShops = new List<Shop>();
			float maxLotCount = wantedList.LotCount();

			if (maxLotCount > 0)
			{
				Shop[] nextBestShops = new Shop[4];
				float[] lotCounts = new float[4];

				for (int i = 0; i < shopCandidates.Count; i++)
				{
					float shopCandidateLotCount = shopCandidates[i].GetShopCoverage(wantedList);

					if (shopCandidateLotCount == maxLotCount)
					{
						topShops.Add(shopCandidates[i]);
					}
					else if (shopCandidateLotCount > 0)
					{
						for (int j = 0; j < nextBestShops.Length; j++)
						{
							if (nextBestShops[j] == null)
							{
								nextBestShops[j] = shopCandidates[i];
								lotCounts[j] = shopCandidateLotCount;
								break;
							}
							else
							{
								if (shopCandidateLotCount > lotCounts[j])
								{
									//Move everything.

									for (int k = nextBestShops.Length - 1; k > j; k--)
									{
										nextBestShops[k] = nextBestShops[k - 1];
										lotCounts[k] = lotCounts[k - 1];
									}

									//and place

									nextBestShops[j] = shopCandidates[i];
									lotCounts[j] = shopCandidateLotCount;
									break;

								}
								else if (shopCandidateLotCount == lotCounts[j])
								{
									if (shopCandidates[i].ShopPriceRank > nextBestShops[j].ShopPriceRank)
									{
										//Move everything

										for (int k = nextBestShops.Length - 1; k < j; k++)
										{
											nextBestShops[k] = nextBestShops[k - 1];
											lotCounts[k] = lotCounts[k - 1];
										}

										//and place

										nextBestShops[j] = shopCandidates[i];
										lotCounts[j] = shopCandidateLotCount;
										break;
									}
								}
							}
						}
					}
				}


				int s = 0;
				while (topShops.Count < 4)
				{
					if (nextBestShops[s] != null)
					{
						topShops.Add(nextBestShops[s]);
						s++;
					}
					else
					{
						break;
					}
				}

			}

			return topShops;
		}


		//----------------------- END ---------------------//
		//------------------- Rarest Lot ------------------//


		private static void RecursiveRarestLotStrategy(
			WantedList wantedList,
			List<Shop> shopCandidates,
			HashSet<Shop> selectedShops,
			float currentProgress,
			float currentPercentage)
		{
			if (actionIsCancelled) return;

			OverwriteDisplay(displayUpdate.ToString() + currentProgress.ToString("00.00") + "%: " +
						uniqueCombinations.Count.ToString() + " additional combinations found, " + combinationsChecked.ToString() + " checked.");
			Thread.Sleep(0);

			//Find the rarest lot, returns null, when wantedlist is empty
			Lot rarestLot = GetRarestLot(wantedList, shopCandidates);

			if (rarestLot != null)
			{
				if (selectedShops.Count == maxShops) return; //Termination

				List<Shop> shopsWithLot = GetShopsWithLot(rarestLot, shopCandidates, wantedList);

				if (shopsWithLot.Count == 0)
				{	//No one have the current rarest lot, set wanted amount to 0 temporarily
					int remainingParts = rarestLot.WantedQuantity;
					rarestLot.ReservedQuantity += remainingParts;

					//and continue
					RecursiveRarestLotStrategy(wantedList, shopCandidates, selectedShops, currentProgress, currentPercentage);

					//Add the part back in
					rarestLot.ReservedQuantity -= remainingParts;
				}
				else
				{
					float newPercentage = currentPercentage / shopsWithLot.Count;

					for (int i = 0; i < shopsWithLot.Count; i++)
					{
						shopsWithLot[i].SimulatePurchase(wantedList);
						shopCandidates.Remove(shopsWithLot[i]);
						selectedShops.Add(shopsWithLot[i]);

						float newProgress = currentProgress + newPercentage * i;

						RecursiveRarestLotStrategy(wantedList, shopCandidates, selectedShops, newProgress, newPercentage);

						shopsWithLot[i].CancelPurchase();
						shopCandidates.Add(shopsWithLot[i]);
						selectedShops.Remove(shopsWithLot[i]);
					}
				}
			}
			else
			{
				if (!uniqueCombinations.Contains(selectedShops))
				{
					uniqueCombinations.Add(new RefHashSet<Shop>(selectedShops));
				}

				combinationsChecked++;
				//if (combinationsChecked % 10 == 0)
				//{
				//    OverwriteDisplay(displayUpdate.ToString() + currentProgress.ToString("00.00") + "%: " +
				//        uniqueCombinations.Count.ToString() + " additional combinations found, " + combinationsChecked.ToString() + " checked.");
				//    Thread.Sleep(0);
				//}
			}
		}

		/// <summary>
		/// Will return null, when wantedList is empty
		/// </summary>
		/// <param name="wantedList"></param>
		/// <param name="shopCandidates"></param>
		/// <returns></returns>
		private static Lot GetRarestLot(WantedList wantedList, List<Shop> shopCandidates)
		{
			Lot rarestLot = null;
			int shopsWithLot = int.MaxValue;
			int shopsWithBrick = int.MaxValue;
			int bricksInExistence = int.MaxValue;
			float rarity = float.MaxValue;

			for (int i = 0; i < wantedList.allLots.Count; i++)
			{
				if (wantedList.allLots[i].WantedQuantity > 0)
				{
					int checkShopsWithLot = 0;
					int checkShopsWithBrick = 0;
					int checkBricksInExistence = 0;

					for (int j = 0; j < shopCandidates.Count; j++)
					{
						int brickCount = shopCandidates[j].GetBrickCount(wantedList.allLots[i].Brick, wantedList.allLots[i].Condition);

						if (brickCount > 0)
						{
							checkBricksInExistence += brickCount;
							checkShopsWithBrick++;
							if (brickCount >= wantedList.allLots[i].WantedQuantity)
							{
								checkShopsWithLot++;
							}
						}
					}

					if (checkShopsWithLot < shopsWithLot)
					{
						rarestLot = wantedList.allLots[i];
						shopsWithLot = checkShopsWithLot;
						shopsWithBrick = checkShopsWithBrick;
						bricksInExistence = checkBricksInExistence;

						if (bricksInExistence > 0)
						{
							rarity = (float)bricksInExistence / (float)wantedList.allLots[i].WantedQuantity;
						}
						else
						{
							rarity = 0;
						}
					}
					else if (checkShopsWithLot == shopsWithLot)
					{
						if (checkShopsWithBrick < shopsWithBrick)
						{
							rarestLot = wantedList.allLots[i];
							shopsWithLot = checkShopsWithLot;
							shopsWithBrick = checkShopsWithBrick;
							bricksInExistence = checkBricksInExistence;
							rarity = bricksInExistence / wantedList.allLots[i].WantedQuantity;
						}
						else if (checkShopsWithBrick == shopsWithBrick)
						{
							if (bricksInExistence / wantedList.allLots[i].WantedQuantity < rarity)
							{
								rarestLot = wantedList.allLots[i];
								shopsWithLot = checkShopsWithLot;
								shopsWithBrick = checkShopsWithBrick;
								bricksInExistence = checkBricksInExistence;
								rarity = bricksInExistence / wantedList.allLots[i].WantedQuantity;
							}
						}
					}
				}
			}


			return rarestLot;
		}

		private static List<Shop> GetShopsWithLot(Lot lot, List<Shop> shopCandidates, WantedList wantedList)
		{
			Shop[] topShops = new Shop[maxDeepth];
			int[] brickCounts = new int[maxDeepth];

			for (int i = 0; i < shopCandidates.Count; i++)
			{
				int brickCount = shopCandidates[i].GetBrickCount(lot.Brick, lot.Condition);

				if (brickCount > 0)
				{
					for (int j = 0; j < topShops.Length; j++)
					{
						if (topShops[j] == null)
						{//Place if empty
							topShops[j] = shopCandidates[i];
							brickCounts[j] = brickCount;
							break;
						}
						else if (
							(brickCount >= lot.WantedQuantity && brickCounts[j] < lot.WantedQuantity) || //C is q while S isnt
							(brickCount < lot.WantedQuantity && brickCounts[j] < lot.WantedQuantity && brickCount > brickCounts[j]) //Both are uq but cb is higer
								)
						{
							goto moveAndPlace;
						}
						else if (
							(brickCount >= lot.WantedQuantity && brickCounts[j] >= lot.WantedQuantity) ||//Both are q
							(brickCount < lot.WantedQuantity && brickCounts[j] < lot.WantedQuantity && brickCount == brickCounts[j])//Both are uq but b is equal
							)
						{
							goto compareByMostLots;
						}
						else
						{
							continue;
						}

					compareByMostLots:

						float selectedShopCoverage = topShops[j].GetShopCoverage(wantedList);
						float candidateShopCoverage = shopCandidates[i].GetShopCoverage(wantedList);

						if (candidateShopCoverage * shopCandidates[i].ShopPriceRank > selectedShopCoverage * topShops[j].ShopPriceRank)
						//if (shopCandidates[i].ShopPriceRank > topShops[j].ShopPriceRank)
						//if (candidateShopCoverage > selectedShopCoverage)
						{
							goto moveAndPlace;
						}
						//else if (selectedShopCoverage == candidateShopCoverage)
						//{
						//    if (shopCandidates[i].ShopPriceRank > topShops[j].ShopPriceRank)
						//    {
						//        goto moveAndPlace;
						//    }
						//}
						else
						{
							continue;
						}

					moveAndPlace:

						for (int k = topShops.Length - 1; k > j; k--)
						{
							topShops[k] = topShops[k - 1];
							brickCounts[k] = brickCounts[k - 1];
						}

						topShops[j] = shopCandidates[i];
						brickCounts[j] = brickCount;
						break;
					}
				}
			}

			List<Shop> shops = new List<Shop>();

			for (int i = 0; i < topShops.Length; i++)
			{
				if (topShops[i] != null)
				{
					shops.Add(topShops[i]);
				}
			}

			return shops;
		}

		//-------------------- END --------------------//

		#region PriceCalc
		private static void CalculateBestCombination()
		{
			for (int i = 0; i < uniqueCombinations.Count; i++)
			{
				OverwriteDisplay(displayUpdate.ToString() + (i + 1).ToString() + " of " + uniqueCombinations.Count.ToString() +
					".\nCheapest offer found: " + (cheapestCompleteOfferPrice * currencyModifier).ToString("0.00") + ".");
				Thread.Sleep(0);

				if (TheorethicalMinimumPrice(uniqueCombinations.ElementAt(i)) < cheapestCompleteOfferPrice)
				{
					CompleteOffer completeOffer = GetCompleteOffer(uniqueCombinations.ElementAt(i));

					if (completeOffer.Price < cheapestCompleteOfferPrice)
					{
						cheapestCompleteOffer = completeOffer;
						cheapestCompleteOfferPrice = completeOffer.Price;
					}
				}
			}
		}

		private static float TheorethicalMinimumPrice(HashSet<Shop> combinationCandidate)
		{
			float minimum = combinationCandidate.Count * shippingCosts;

			for (int i = 0; i < combinationCandidate.Count; i++)
			{
				minimum += combinationCandidate.ElementAt(i).MinimumBuy;
			}

			return minimum;
		}

		private static CompleteOffer GetCompleteOffer(HashSet<Shop> shops)
		{
			//STAGE I: Hand out lots to the shop with the lowest unitprice.
			//If there is shops with unmeet minbuys that can also deliver, yet are not the cheapest, wait for stage II.
			StageI(DAO.GetWantedList().newLots, shops);
			StageI(DAO.GetWantedList().nanLots, shops);

			//STAGE II: Hand out lots to the unmeet minbuy shops with the lowest price, but only until meet.
			StageII(DAO.GetWantedList().newLots, shops);
			StageII(DAO.GetWantedList().nanLots, shops);

			//STAGE III: Hand out any remaining lots to whoever is cheapest.
			StageIII(DAO.GetWantedList().newLots, shops);
			StageIII(DAO.GetWantedList().nanLots, shops);

			float completePrice = 0;
			IDictionary<Shop, List<Offer>> shopOffers = new Dictionary<Shop, List<Offer>>();

			for (int i = 0; i < shops.Count; i++)
			{
				if (shops.ElementAt(i).SimulatedIsMinimumBuyMeet)
				{
					completePrice += shops.ElementAt(i).SimulatedTotal;
				}
				else
				{
					completePrice += shops.ElementAt(i).MinimumBuy;
				}

				shopOffers[shops.ElementAt(i)] = shops.ElementAt(i).CancelPurchaseAndReturnOffers();
			}

			//Add shipping
			completePrice += shops.Count * shippingCosts;

			return new CompleteOffer(completePrice, shopOffers);
		}

		private static void StageI(List<Lot> requestedLots, HashSet<Shop> shops)
		{
			for (int i = 0; i < requestedLots.Count; i++)
			{
			start:
				Offer cheapestOffer = null;
				int totalOffers = 0;
				bool noMinBuys = true;

				for (int j = 0; j < shops.Count; j++)
				{
					//WantedQuantity will ALWAYS be > 0 at this stage
					Offer offerCandidate = shops.ElementAt(j).GetOffer(requestedLots[i]);
					if (offerCandidate != null)
					{
						totalOffers++;
						if (!shops.ElementAt(j).SimulatedIsMinimumBuyMeet)
						{ //Offer from a shop with an unmeet minBuy
							noMinBuys = false;
						}

						if (cheapestOffer == null)
						{
							cheapestOffer = offerCandidate;
						}
						else
						{
							if (offerCandidate.CalculatedUnitPrice < cheapestOffer.CalculatedUnitPrice)
							{
								cheapestOffer = offerCandidate;
							}
						}
					}
				}

				if (totalOffers == 0)
				{//If no offers - continue the loop as normal.
					continue;
				}
				else if (totalOffers == 1)
				{ //Only one offer for this lot, select it
					cheapestOffer.SimulatePurchase(true);

					//The selected shop might have more offers for this lot though
					if (requestedLots[i].WantedQuantity > 0)
					{ //More offers availible and more parts needed - rewind to beginning.
						goto start;
					}
				}
				else if (totalOffers > 1)
				{//More than one offer.
					if (noMinBuys || cheapestOffer.ShopHasUnmeetMinBuy)
					{ //There are no minBuyOffers or best one is from a minbuy shop
						cheapestOffer.SimulatePurchase(true);

						if (requestedLots[i].WantedQuantity > 0)
						{ //More offers availible and more parts needed - rewind to beginning.
							goto start;
						}
					}
					//In all other instances, wait until stage II with handing out the lot
				}
			}
		}

		private static void StageII(List<Lot> requestedLots, HashSet<Shop> shops)
		{
			for (int i = 0; i < requestedLots.Count; i++)
			{
				if (requestedLots[i].WantedQuantity > 0)
				{
				start:
					Offer cheapestOffer = null;

					for (int j = 0; j < shops.Count; j++)
					{
						if (!shops.ElementAt(j).SimulatedIsMinimumBuyMeet)
						{
							Offer offerCandidate = shops.ElementAt(j).GetOffer(requestedLots[i]);

							if (offerCandidate != null)
							{
								if (cheapestOffer == null)
								{
									cheapestOffer = offerCandidate;
								}
								else
								{
									if (offerCandidate.CalculatedUnitPrice < cheapestOffer.CalculatedUnitPrice)
									{
										cheapestOffer = offerCandidate;
									}
									else if (offerCandidate.CalculatedUnitPrice == cheapestOffer.CalculatedUnitPrice)
									{ //Give to the one furthest away from its target
										if (offerCandidate.ShopAmountToReachMinBuy > cheapestOffer.ShopAmountToReachMinBuy)
										{
											cheapestOffer = offerCandidate;
										}
									}
								}
							}
						}
					}

					if (cheapestOffer != null)
					{
						cheapestOffer.SimulatePurchase(true);

						if (requestedLots[i].WantedQuantity > 0)
						{ //Rewind if more is needed, if no more offers it won´t end here again
							goto start;
						}
					}
				}
			}
		}

		private static void StageIII(List<Lot> requestedLots, HashSet<Shop> shops)
		{
			for (int i = 0; i < requestedLots.Count; i++)
			{
				if (requestedLots[i].WantedQuantity > 0)
				{
				start:
					Offer cheapestOffer = null;

					for (int j = 0; j < shops.Count; j++)
					{
						Offer offerCandidate = shops.ElementAt(j).GetOffer(requestedLots[i]);

						if (offerCandidate != null)
						{
							if (cheapestOffer == null)
							{
								cheapestOffer = offerCandidate;
							}
							else
							{
								if (offerCandidate.CalculatedUnitPrice < cheapestOffer.CalculatedUnitPrice)
								{
									cheapestOffer = offerCandidate;
								}
							}
						}
					}

					if (cheapestOffer != null)
					{
						cheapestOffer.SimulatePurchase(true);

						if (requestedLots[i].WantedQuantity > 0)
						{ //Rewind if more is needed.
							goto start;
						}
					}
				}
			}
		}

		#endregion
	}
}