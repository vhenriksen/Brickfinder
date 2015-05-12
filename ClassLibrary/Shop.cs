using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Model
{
	public class Shop
	{
		public string Name { get; private set; }
		public string Country { get; private set; }
		public float MinimumBuy { get; private set; }
		public IDictionary<Brick, List<ShopItem>> NewInventory { get; private set; }
		public IDictionary<Brick, List<ShopItem>> AllInventory { get; private set; }
		public float ShopPriceRank { get; private set; }
		public bool IsBlacklisted { get; private set; }

		//Offers that are beeing simulated
		private List<Offer> simulatedOffers = new List<Offer>();

		//Only used in lotoffer price calculation.
		public float SimulatedTotal { get; set; }
		public float SimulatedRemainingMinimumBuy { get { return MinimumBuy - SimulatedTotal; } }
		public bool SimulatedIsMinimumBuyMeet { get { return SimulatedTotal >= MinimumBuy; } }

		public Shop(string name, string country, float minimumBuy)
		{
			Name = name;
			Country = country;
			MinimumBuy = minimumBuy;
			NewInventory = new Dictionary<Brick, List<ShopItem>>();
			AllInventory = new Dictionary<Brick, List<ShopItem>>();
			IsBlacklisted = false;
		}

		public void CreateShopItem(Brick brick, float price, Condition condition, int quantity, int multiplier, string description)
		{
			ShopItem shopItem = new ShopItem(brick, price, condition, quantity, multiplier, description);

			if (!AllInventory.ContainsKey(brick))
			{
				AllInventory[brick] = new List<ShopItem>();
			}
			AllInventory[brick].Add(shopItem);

			if (condition == Condition.New)
			{
				if (!NewInventory.ContainsKey(brick))
				{
					NewInventory[brick] = new List<ShopItem>();
				}
				NewInventory[brick].Add(shopItem);
			}
		}

		#region Coverage

		public float GetShopCoverage(WantedList wantedList)
		{
			float shopCoverage = 0;

			//New first
			for (int i = 0; i < wantedList.newLots.Count; i++)
			{
				if (wantedList.newLots[i].WantedQuantity > 0)
				{
					if (NewInventory.ContainsKey(wantedList.newLots[i].Brick))
					{
						shopCoverage += GetLotCoverage(NewInventory[wantedList.newLots[i].Brick], wantedList.newLots[i]);
					}
				}
			}

			//NaN second
			for (int i = 0; i < wantedList.nanLots.Count; i++)
			{
				if (wantedList.nanLots[i].WantedQuantity > 0)
				{
					if (AllInventory.ContainsKey(wantedList.nanLots[i].Brick))
					{
						shopCoverage += GetLotCoverage(AllInventory[wantedList.nanLots[i].Brick], wantedList.nanLots[i]);
					}
				}
			}

			//Reset all amounts before returning.
			CancelPurchase();

			return shopCoverage;
		}

		/// <summary>
		/// Returns the coverage for this lot. 
		/// Note: Changes availibility of shop items!
		/// </summary>
		/// <param name="shopItemCandidates"></param>
		/// <param name="requestedQuantity"></param>
		/// <returns></returns>
		private float GetLotCoverage(List<ShopItem> shopItemCandidates, Lot requestedLot)
		{
			float originalWantedQuantity = requestedLot.WantedQuantity;

			while (requestedLot.WantedQuantity > 0)
			{
				ShopItem cheapestShopItem = GetCheapestShopItem(shopItemCandidates, requestedLot.WantedQuantity);

				if (cheapestShopItem != null)
				{
					int quantityAvailible = cheapestShopItem.MultiplierAdjustedQuantityAvailible(requestedLot.WantedQuantity);
					Offer offer = new Offer(this, requestedLot, cheapestShopItem, quantityAvailible);
					offer.SimulatePurchase(true);
				}
				else
				{
					break;
				}
			}

			if (originalWantedQuantity != requestedLot.WantedQuantity)
			{
				if (requestedLot.WantedQuantity <= 0)
				{
					return 1;
				}
				else
				{
					return 1 - (requestedLot.WantedQuantity / originalWantedQuantity);
				}
			}
			else
			{
				return 0;
			}
		}

		/// <summary>
		/// Find and returns the cheapest shopitem
		/// </summary>
		/// <param name="shopItemCandidates"></param>
		/// <param name="requestedQuantity"></param>
		/// <returns></returns>
		private ShopItem GetCheapestShopItem(List<ShopItem> shopItemCandidates, int requestedQuantity)
		{
			ShopItem cheapestShopItem = null;
			float lowestUnitPrice = float.MaxValue;

			//Check each of them for the lowest unit price
			for (int i = 0; i < shopItemCandidates.Count; i++)
			{
				int quantityToBuy = shopItemCandidates[i].MultiplierAdjustedQuantityAvailible(requestedQuantity);

				//But only if bricks are actually availible for purchase
				if (quantityToBuy > 0)
				{
					float unitPrice;

					if (quantityToBuy > requestedQuantity)
					{
						unitPrice = (quantityToBuy * shopItemCandidates[i].UnitPrice) / requestedQuantity;
					}
					else
					{
						unitPrice = shopItemCandidates[i].UnitPrice;
					}

					if (unitPrice < lowestUnitPrice)
					{
						cheapestShopItem = shopItemCandidates[i];
						lowestUnitPrice = unitPrice;
					}
				}
			}

			return cheapestShopItem;
		}

		#endregion

		#region IndividualOffers

		/// <summary>
		/// Returns the best offer on this lot that can be delivered right now
		/// </summary>
		/// <param name="requestedLot"></param>
		/// <returns></returns>
		public Offer GetOffer(Lot requestedLot)
		{
			if (requestedLot.WantedQuantity > 0)
			{
				if (requestedLot.Condition == Condition.New)
				{
					if (NewInventory.ContainsKey(requestedLot.Brick))
					{
						ShopItem cheapestShopItem = GetCheapestShopItem(NewInventory[requestedLot.Brick], requestedLot.WantedQuantity);
						if (cheapestShopItem != null)
						{
							int quantityToBuy = cheapestShopItem.MultiplierAdjustedQuantityAvailible(requestedLot.WantedQuantity);
							return new Offer(this, requestedLot, cheapestShopItem, quantityToBuy);
						}
					}
				}
				else
				{
					if (AllInventory.ContainsKey(requestedLot.Brick))
					{
						ShopItem cheapestShopItem = GetCheapestShopItem(AllInventory[requestedLot.Brick], requestedLot.WantedQuantity);
						if (cheapestShopItem != null)
						{
							int quantityToBuy = cheapestShopItem.MultiplierAdjustedQuantityAvailible(requestedLot.WantedQuantity);
							return new Offer(this, requestedLot, cheapestShopItem, quantityToBuy);
						}
					}
				}

			}

			return null;
		}

		/// <summary>
		/// Used for adding simulated offers to the internal list
		/// Can only be called by the offer itself
		/// </summary>
		/// <param name="offer"></param>
		public void AddSimulatedPurchase(Offer offer)
		{
			simulatedOffers.Add(offer);
		}

		#endregion

		#region SimulatePurchase

		/// <summary>
		/// Buys as much as possible in the wantedlist. Can be cancelled by calling the store.
		/// </summary>
		/// <param name="wantedList"></param>
		public void SimulatePurchase(WantedList wantedList)
		{
			//New first
			for (int i = 0; i < wantedList.newLots.Count; i++)
			{
				if (NewInventory.ContainsKey(wantedList.newLots[i].Brick))
				{
					SimulateLotPurchase(NewInventory[wantedList.newLots[i].Brick], wantedList.newLots[i]);
				}
			}

			//Used second
			for (int i = 0; i < wantedList.nanLots.Count; i++)
			{
				if (AllInventory.ContainsKey(wantedList.nanLots[i].Brick))
				{
					SimulateLotPurchase(AllInventory[wantedList.nanLots[i].Brick], wantedList.nanLots[i]);
				}
			}
		}

		/// <summary>
		/// Creates and simulates offers for this lot
		/// </summary>
		/// <param name="shopItemCandidates"></param>
		/// <param name="requestedLot"></param>
		private void SimulateLotPurchase(List<ShopItem> shopItemCandidates, Lot requestedLot)
		{
			while (requestedLot.WantedQuantity > 0)
			{
				ShopItem cheapestShopItem = GetCheapestShopItem(shopItemCandidates, requestedLot.WantedQuantity);

				if (cheapestShopItem != null)
				{
					int quantityAvailible = cheapestShopItem.MultiplierAdjustedQuantityAvailible(requestedLot.WantedQuantity);
					Offer offer = new Offer(this, requestedLot, cheapestShopItem, quantityAvailible);
					offer.SimulatePurchase(true);
				}
				else
				{
					break;
				}
			}
		}

		public void CancelPurchase()
		{
			for (int i = 0; i < simulatedOffers.Count; i++)
			{
				simulatedOffers[i].SimulatePurchase(false);
			}

			simulatedOffers = new List<Offer>();
			SimulatedTotal = 0;
		}

		public List<Offer> CancelPurchaseAndReturnOffers()
		{
			for (int i = 0; i < simulatedOffers.Count; i++)
			{
				simulatedOffers[i].SimulatePurchase(false);
			}

			List<Offer> offers = simulatedOffers;
			simulatedOffers = new List<Offer>();
			SimulatedTotal = 0;
			return offers;
		}

		#endregion

		#region ShopRank

		//For price index
		public void AddBrickPrices(
			IDictionary<Brick, SortedDictionary<float, int>> newBrickPrices,
			IDictionary<Brick, SortedDictionary<float, int>> usedBrickPrices,
			IDictionary<Brick, int> newBrickAmounts,
			IDictionary<Brick, int> usedBrickAmounts
			)
		{
			for (int i = 0; i < AllInventory.Count; i++)
			{
				for (int j = 0; j < AllInventory.ElementAt(i).Value.Count; j++)
				{
					if (AllInventory.ElementAt(i).Value.ElementAt(j).Condition == Condition.New)
					{
						for (int l = 0; l < AllInventory.ElementAt(i).Value.Count; l++)
						{
							if (!newBrickPrices[AllInventory.ElementAt(i).Key].ContainsKey(AllInventory.ElementAt(i).Value[l].UnitPrice))
							{
								newBrickPrices[AllInventory.ElementAt(i).Key].Add(AllInventory.ElementAt(i).Value[l].UnitPrice, 0);
							}

							newBrickPrices[AllInventory.ElementAt(i).Key][AllInventory.ElementAt(i).Value[l].UnitPrice] += AllInventory.ElementAt(i).Value[l].QuantityAvailible;
							newBrickAmounts[AllInventory.ElementAt(i).Key] += AllInventory.ElementAt(i).Value[l].QuantityAvailible;
						}
					}
					else
					{
						for (int l = 0; l < AllInventory.ElementAt(i).Value.Count; l++)
						{
							if (!usedBrickPrices[AllInventory.ElementAt(i).Key].ContainsKey(AllInventory.ElementAt(i).Value[l].UnitPrice))
							{
								usedBrickPrices[AllInventory.ElementAt(i).Key].Add(AllInventory.ElementAt(i).Value[l].UnitPrice, 0);
							}

							usedBrickPrices[AllInventory.ElementAt(i).Key][AllInventory.ElementAt(i).Value[l].UnitPrice] += AllInventory.ElementAt(i).Value[l].QuantityAvailible;
							usedBrickAmounts[AllInventory.ElementAt(i).Key] += AllInventory.ElementAt(i).Value[l].QuantityAvailible;
						}
					}
				}
			}
		}

		//Points awarded depending on brick availibility > 0 only
		public void CalculateShopRankI(
			IDictionary<Brick, float> newBrickPriceIndex,
			IDictionary<Brick, float> usedBrickPriceIndex,
			WantedList wantedList)
		{
			//Reset score
			ShopPriceRank = 0;

			//New first
			for (int i = 0; i < wantedList.newLots.Count; i++)
			{
				if (NewInventory.ContainsKey(wantedList.newLots[i].Brick))
				{
					ShopItem cheapestShopItem = GetCheapestShopItem(NewInventory[wantedList.newLots[i].Brick], 1);
					if (cheapestShopItem != null)
					{
						int quantityAvailible = cheapestShopItem.MultiplierAdjustedQuantityAvailible(1);

						if (quantityAvailible >= 1)
						{
							Offer offer = new Offer(this, wantedList.newLots[i], cheapestShopItem, quantityAvailible);
							offer.SimulatePurchase(true);
							ShopPriceRank += newBrickPriceIndex[wantedList.newLots[i].Brick] / cheapestShopItem.UnitPrice;
						}
					}
				}
			}

			//NaN second
			for (int i = 0; i < wantedList.nanLots.Count; i++)
			{
				if (AllInventory.ContainsKey(wantedList.nanLots[i].Brick))
				{
					ShopItem cheapestShopItem = GetCheapestShopItem(AllInventory[wantedList.nanLots[i].Brick], 1);
					if (cheapestShopItem != null)
					{
						int quantityAvailible = cheapestShopItem.MultiplierAdjustedQuantityAvailible(1);

						if (quantityAvailible >= 1)
						{
							Offer offer = new Offer(this, wantedList.nanLots[i], cheapestShopItem, quantityAvailible);
							offer.SimulatePurchase(true);

							if (cheapestShopItem.Condition == Condition.New)
							{
								ShopPriceRank += newBrickPriceIndex[wantedList.nanLots[i].Brick] / cheapestShopItem.UnitPrice;
							}
							else
							{
								ShopPriceRank += usedBrickPriceIndex[wantedList.nanLots[i].Brick] / cheapestShopItem.UnitPrice;
							}
						}
					}
				}
			}

			CancelPurchase();
		}

		//Points awarded depending on brick availibility compared to requested amount
		public void CalculateShopRankII(
			IDictionary<Brick, float> newBrickPriceIndex,
			IDictionary<Brick, float> usedBrickPriceIndex,
			WantedList wantedList)
		{
			//Reset score
			ShopPriceRank = 0;

			//New first
			for (int i = 0; i < wantedList.newLots.Count; i++)
			{
				if (NewInventory.ContainsKey(wantedList.newLots[i].Brick))
				{
					ShopItem cheapestShopItem = GetCheapestShopItem(NewInventory[wantedList.newLots[i].Brick], wantedList.newLots[i].WantedQuantity);
					if (cheapestShopItem != null)
					{
						int quantityAvailible = cheapestShopItem.MultiplierAdjustedQuantityAvailible(wantedList.newLots[i].WantedQuantity);

						if (quantityAvailible >= 1)
						{
							float scoreMultiplier = quantityAvailible >= wantedList.newLots[i].WantedQuantity
								? 1
								: quantityAvailible / wantedList.newLots[i].WantedQuantity;

							Offer offer = new Offer(this, wantedList.newLots[i], cheapestShopItem, quantityAvailible);
							offer.SimulatePurchase(true);
							ShopPriceRank += (newBrickPriceIndex[wantedList.newLots[i].Brick] / cheapestShopItem.UnitPrice) * scoreMultiplier;
						}
					}
				}
			}

			//NaN second
			for (int i = 0; i < wantedList.nanLots.Count; i++)
			{
				if (AllInventory.ContainsKey(wantedList.nanLots[i].Brick))
				{
					ShopItem cheapestShopItem = GetCheapestShopItem(AllInventory[wantedList.nanLots[i].Brick], wantedList.nanLots[i].WantedQuantity);
					if (cheapestShopItem != null)
					{
						int quantityAvailible = cheapestShopItem.MultiplierAdjustedQuantityAvailible(1);

						if (quantityAvailible >= 1)
						{
							float scoreMultiplier = quantityAvailible >= wantedList.nanLots[i].WantedQuantity
								? 1
								: quantityAvailible / wantedList.nanLots[i].WantedQuantity;

							Offer offer = new Offer(this, wantedList.nanLots[i], cheapestShopItem, quantityAvailible);
							offer.SimulatePurchase(true);

							if (cheapestShopItem.Condition == Condition.New)
							{
								ShopPriceRank += (newBrickPriceIndex[wantedList.nanLots[i].Brick] / cheapestShopItem.UnitPrice) * scoreMultiplier;
							}
							else
							{
								ShopPriceRank += (usedBrickPriceIndex[wantedList.nanLots[i].Brick] / cheapestShopItem.UnitPrice) * scoreMultiplier;
							}
						}
					}
				}
			}

			CancelPurchase();
		}

		#endregion

		public int GetBrickCount(Brick brick, Condition condition)
		{
			int brickCount = 0;

			if (NewInventory.ContainsKey(brick))
			{
				for (int i = 0; i < NewInventory[brick].Count; i++)
				{
					brickCount += NewInventory[brick][i].QuantityAvailible;
				}
			}

			if (condition == Condition.NaN)
			{
				if (AllInventory.ContainsKey(brick))
				{
					for (int i = 0; i < AllInventory[brick].Count; i++)
					{
						brickCount += AllInventory[brick][i].QuantityAvailible;
					}
				}
			}

			return brickCount;
		}

		public override bool Equals(object obj)
		{
			Shop shop = obj as Shop;

			if (obj != null)
			{
				return Name == shop.Name && Country == shop.Country;
			}
			else
			{
				return false;
			}
		}

		public string ToString(float currencyModifier)
		{
			return Name +
				", " + Country +
				(MinimumBuy > 0 ? ", Minimum buy " + (MinimumBuy * currencyModifier).ToString("0.00") + "," : ",") +
				" with " + NewInventory.Count().ToString() + " unique bricks(new and used counted as 1)";
		}

		public override int GetHashCode()
		{
			//from http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
			int hash = 17;

			hash = hash * 29 + Name.GetHashCode();
			hash = hash * 29 + Country.GetHashCode();

			return hash;
		}


		public void UpdateBlacklistStatus(List<string> shopBlacklist, List<string> wordBlacklist)
		{
			//First check that the name isnt blacklisted
			//Its important, that its whats typed in, in the blacklist thats compared to the collected name
			//The typed in name can then be shorter if nessesary ex:
			//
			//"Wallakovicky" would be blacklisted if the word "Walla" appeared in the blacklist
			foreach (string nameToBlacklist in shopBlacklist)
			{
				if (Name.Contains(nameToBlacklist))
				{
					IsBlacklisted = true;
					break;
				}
			}

			//Only if not blacklisted go on to the word blacklist for lots
			if (!IsBlacklisted)
			{
				for (int i = 0; i < AllInventory.Count; i++)
				{
					for (int j = 0; j < AllInventory.ElementAt(i).Value.Count; j++)
					{
						AllInventory.ElementAt(i).Value[j].UpdateBlacklistStatus(wordBlacklist);
					}
				}
			}
		}

		public void SetMaxPercentageOverMedianPrice(bool isEnabled, float percentageMax, IDictionary<Brick, float> newMedianBrickPrices, IDictionary<Brick, float> usedMedianBrickPrices)
		{
			for (int i = 0; i < AllInventory.Count; i++)
			{
				for (int j = 0; j < AllInventory.ElementAt(i).Value.Count; j++)
				{
					ShopItem shopItem = AllInventory.ElementAt(i).Value[j];

					float maximumUnitPrice = shopItem.Condition == Condition.New ? newMedianBrickPrices[shopItem.Brick] * percentageMax : usedMedianBrickPrices[shopItem.Brick] * percentageMax;
					shopItem.UpdatePriceExclusionStatus(maximumUnitPrice, isEnabled);

				}
			}
		}
	}

}
