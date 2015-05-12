using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
	public class CompleteOffer
	{
		public float Price { get; private set; }
		private IDictionary<Shop, List<Offer>> shopOffers;

		public CompleteOffer(float price, IDictionary<Shop, List<Offer>> shopOffers)
		{
			this.Price = price;
			this.shopOffers = shopOffers;
		}

		public void Simulate(bool simulate)
		{
			if (simulate)
			{
				for (int i = 0; i < shopOffers.Count; i++)
				{
					for (int j = 0; j < shopOffers.ElementAt(i).Value.Count; j++)
					{
						shopOffers.ElementAt(i).Value[j].SimulatePurchase(true);
					}
				}
			}
			else
			{
				for (int i = 0; i < shopOffers.Count; i++)
				{
					shopOffers.ElementAt(i).Key.CancelPurchase();
				}
			}
		}

		public string ToString(float shipping, float currencyModifier, bool showDescriptions)
		{
			//This method is only called once, when the simulation is over so a bit of unoptimized code is allowed.

			float baseprice = 0;
			float minimumbuys = 0;
			float multiplierAddedPrices = 0;
			float totalShipping = 0;
			int totalMultiplierAddedBricks = 0;
			int totalBricks = 0;
			int totalLotCount = 0;

			StringBuilder allInfo = new StringBuilder();

			//Run through each shops offers
			for (int i = 0; i < shopOffers.Count; i++)
			{
				float subBase = 0;
				float subMinBuy = 0;
				float subMultiplierAddedPrice = 0;
				float subShipping = shipping;
				int subBricks = 0;
				int subMultiplierAddedBricks = 0;
				int subLotCount = 0;

				float subWeight = 0;

				//Run through each offer from the shop
				StringBuilder offerInfo = new StringBuilder();
				shopOffers.ElementAt(i).Value.Sort();
				for (int j = 0; j < shopOffers.ElementAt(i).Value.Count; j++)
				{
					offerInfo.Append(shopOffers.ElementAt(i).Value[j].ToString(currencyModifier, showDescriptions) + "\n");
					subBricks += shopOffers.ElementAt(i).Value[j].AquiredQuantity;
					subMultiplierAddedBricks += shopOffers.ElementAt(i).Value[j].MultiplierAddedAmount;
					subLotCount++;
					subBase += shopOffers.ElementAt(i).Value[j].BasePrice;
					subMultiplierAddedPrice += shopOffers.ElementAt(i).Value[j].MultiplierAddedPrice;

					subWeight += shopOffers.ElementAt(i).Value[j].Weight;
				}

				//Check for minBuy
				if ((subBase + subMultiplierAddedPrice) < shopOffers.ElementAt(i).Key.MinimumBuy)
				{
					subMinBuy = shopOffers.ElementAt(i).Key.MinimumBuy - subBase - subMultiplierAddedPrice;
				}

				//Update totals
				baseprice += subBase;
				minimumbuys += subMinBuy;
				multiplierAddedPrices += subMultiplierAddedPrice;
				totalBricks += subBricks;
				totalMultiplierAddedBricks += subMultiplierAddedBricks;
				totalLotCount += subLotCount;
				totalShipping += subShipping;

				float subTotal = subBase + subMinBuy + subMultiplierAddedPrice + subShipping;

				//Conversion ok
				float avgBaseBrickPrice = (subBase + subShipping) / (subBricks - subMultiplierAddedBricks);
				float avgBrickPrice = subTotal / subBricks;

				//Name of shop
				allInfo.Append(shopOffers.ElementAt(i).Key.ToString(currencyModifier) + "\n");

				//How many bricks in how many lots
				allInfo.Append(subBricks.ToString() + " bricks in " + subLotCount.ToString() + " lots:\n");

				//Baseprice
				allInfo.Append("+ " + (subBase * currencyModifier).ToString("0.00") + " base price.\n");

				//MultiplierAddedBricks if any
				if (subMultiplierAddedBricks > 0)
				{
					allInfo.Append("+ " + (subMultiplierAddedPrice * currencyModifier).ToString("0.00") + " to meet multipliers (" + ((subMultiplierAddedPrice / subTotal) * 100).ToString("0") + "%).\n");
				}

				//MinBuys if any
				if (subMinBuy > 0)
				{
					allInfo.Append("+ " + (subMinBuy * currencyModifier).ToString("0.00") + " to meet minimum buy (" + ((subMinBuy / subTotal) * 100).ToString("0") + "%).\n");
				}

				//Shipping
				allInfo.Append("+ " + (subShipping * currencyModifier).ToString("0.00") + " shipping (" + ((subShipping / subTotal) * 100).ToString("0") + "%).\n");

				//Total
				allInfo.Append("= " + (subTotal * currencyModifier).ToString("0.00") +
					" ~ " + (avgBrickPrice * currencyModifier).ToString("0.00") + " pr. brick (" + (avgBaseBrickPrice * currencyModifier).ToString("0.00") + " w/o restrictions).\n\n");

				allInfo.Append("Weight: " + subWeight.ToString("0") +"g\n\n");

				allInfo.Append(offerInfo);

				allInfo.Append("\n\n");
			}

			//Conversion ok
			float totalprice = baseprice + multiplierAddedPrices + minimumbuys + totalShipping;
			float avgTotalBaseBrickPrice = (baseprice + totalShipping) / (totalBricks - totalMultiplierAddedBricks);
			float avgTotalBrickPrice = totalprice / totalBricks;

			allInfo.Insert(0, "------------------------------------------------------\n\n\n");
			allInfo.Insert(0, "= " + (totalprice * currencyModifier).ToString("0.00") +
				" ~ " + (avgTotalBrickPrice * currencyModifier).ToString("0.00") +
				" pr. brick (" + (avgTotalBaseBrickPrice * currencyModifier).ToString("0.00") + " w/o restrictions).\n");

			allInfo.Insert(0, "+ " + (totalShipping * currencyModifier).ToString("0.00") + " shipping (" + ((totalShipping / totalprice) * 100).ToString("0") + "%).\n");

			if (minimumbuys > 0)
			{
				allInfo.Insert(0, "+ " + (minimumbuys * currencyModifier).ToString("0.00") + " to meet minimum buy (" + ((minimumbuys / totalprice) * 100).ToString("0") + "%).\n");
			}

			if (totalMultiplierAddedBricks > 0)
			{
				allInfo.Insert(0, "+ " + (multiplierAddedPrices * currencyModifier).ToString("0.00") + " to meet multipliers (" + ((multiplierAddedPrices / totalprice) * 100).ToString("0") + "%).\n");
			}

			allInfo.Insert(0, "+ " + (baseprice * currencyModifier).ToString("0.00") + " base price.\n");

			allInfo.Insert(0, totalBricks.ToString() + " bricks in " + totalLotCount.ToString() + " lots in " + shopOffers.Count.ToString() + " shops.\n");

			allInfo.Insert(0, "------ Overview -----------------------------------\n");

			return allInfo.ToString();
		}
	}
}
