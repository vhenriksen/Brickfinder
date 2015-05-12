using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
	public class Offer : IComparable
	{
		private Shop shop;
		public bool ShopHasUnmeetMinBuy { get { return !shop.SimulatedIsMinimumBuyMeet; } }
		public float ShopAmountToReachMinBuy { get { return shop.SimulatedRemainingMinimumBuy; } }
		private Lot lot;
		private ShopItem shopItem;
		public int AquiredQuantity { get; private set; }
		public int RequestedQuantity { get; private set; }
		public int MultiplierAddedAmount { get; private set; }
		public float MultiplierAddedPrice { get; private set; }
		private bool simulating = false;
		public float UnitPrice { get; private set; }
		public float CalculatedUnitPrice { get; private set; }
		public float TotalPrice { get { return BasePrice + MultiplierAddedPrice; } }
		public float BasePrice { get; private set; }
		public float Weight { get; private set; }

		public Offer(Shop shop, Lot lot, ShopItem shopItem, int amount)
		{
			this.shop = shop;
			this.lot = lot;
			this.shopItem = shopItem;
			AquiredQuantity = amount;
			RequestedQuantity = lot.WantedQuantity;
			UnitPrice = shopItem.UnitPrice;

			Weight = AquiredQuantity * lot.Brick.Weight;

			if (AquiredQuantity <= RequestedQuantity)
			{
				CalculatedUnitPrice = shopItem.UnitPrice;
				MultiplierAddedAmount = 0;
				MultiplierAddedPrice = 0;
				BasePrice = UnitPrice * AquiredQuantity;
			}
			else
			{//Converts correctly.
				CalculatedUnitPrice = (AquiredQuantity * UnitPrice) / (float)RequestedQuantity;
				MultiplierAddedAmount = AquiredQuantity - RequestedQuantity;
				MultiplierAddedPrice = (float)MultiplierAddedAmount * UnitPrice;
				BasePrice = UnitPrice * RequestedQuantity;
			}
		}

		public void SimulatePurchase(bool simulate)
		{
			if (simulate)
			{
				if (simulating)
				{
					throw new Exception("Already simulating.");
				}
				else
				{
					lot.ReservedQuantity += AquiredQuantity;
					shopItem.ReservedQuantity += AquiredQuantity;
					shop.SimulatedTotal += CalculatedUnitPrice * AquiredQuantity;
					shop.AddSimulatedPurchase(this);

					simulating = true;
				}
			}
			else
			{
				if (!simulating)
				{
					throw new Exception("Already not simulating.");
				}
				else
				{
					lot.ReservedQuantity -= AquiredQuantity;
					shopItem.ReservedQuantity -= AquiredQuantity;
					shop.SimulatedTotal -= CalculatedUnitPrice * AquiredQuantity;

					simulating = false;
				}
			}
		}

		public string ToString(float currencyModifier, bool showDescription)
		{
			StringBuilder sb = new StringBuilder(AquiredQuantity.ToString());

			if (MultiplierAddedAmount > 0)
			{
				sb.Append("(" + MultiplierAddedAmount.ToString() + ")x ");
			}
			else
			{
				sb.Append("x");
			}

			sb.Append((UnitPrice * currencyModifier).ToString("0.00"));

			if (shopItem.Condition == Condition.New)
			{
				sb.Append(" (N) = ");
			}
			else
			{
				sb.Append(" (U) = ");
			}

			sb.Append((TotalPrice * currencyModifier).ToString("0.00") + " : ");

			sb.Append(shopItem.Brick.ToString());

			if (showDescription && !string.IsNullOrEmpty(shopItem.Description.Trim()))
			{
				sb.Append(", " + shopItem.Description);
			}

			return sb.ToString();
		}

		int IComparable.CompareTo(object obj)
		{
			Offer o = (Offer)obj;

			//Order by lowest Id first
			if (o.shopItem.Brick.BricklinkId < this.shopItem.Brick.BricklinkId)
			{
				return 1;
			}
			//Order secondly by color
			else if (o.shopItem.Brick.BricklinkId == this.shopItem.Brick.BricklinkId)
			{
				if (o.shopItem.Brick.BricklinkColorId > this.shopItem.Brick.BricklinkColorId)
				{
					return 1;
				}
				else if (o.shopItem.Brick.BricklinkColorId == this.shopItem.Brick.BricklinkColorId)
				{
					return 0;
				}
				else
				{
					return -1;
				}
			}
			else
			{
				return -1;
			}
		}
	}
}
