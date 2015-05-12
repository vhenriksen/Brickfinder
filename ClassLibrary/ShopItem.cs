using System;
using System.Collections.Generic;
namespace Model
{
	public class ShopItem
	{
		public Brick Brick { get; private set; }
		public float UnitPrice { get; private set; }
		public Condition Condition { get; private set; }
		private int Quantity { get; set; }
		public int ReservedQuantity { get; set; }//ONLY for SHOPS to mess with when a simulated purchase is made.
		public int QuantityAvailible { get { return Quantity - ReservedQuantity; } }
		public int Multiplier { get; private set; }
		public string Description { get; private set; }
		public bool Blacklisted { get; private set; }
		public bool IsPriceExcluded { get; set; }

		public ShopItem(Brick brick, float unitPrice, Condition condition, int quantity, int multiplier, string description)
		{
			Brick = brick;
			UnitPrice = unitPrice;
			Condition = condition;
			Quantity = quantity;
			ReservedQuantity = 0;
			Multiplier = multiplier;
			Description = description;
			Blacklisted = false;
		}

		//Returns the closest quantity that can be bought
		public int MultiplierAdjustedQuantityAvailible(int requestedQuantity)
		{
			if (Blacklisted || IsPriceExcluded) return 0;

			int adjustedQuantity = requestedQuantity;

			//Adjust the wantedQuantity up until the multiplier matches
			while (adjustedQuantity % Multiplier != 0)
			{
				adjustedQuantity++;
			}

			//Adjust the wantedQuantity down until a sale is possible
			while (adjustedQuantity > QuantityAvailible)
			{
				adjustedQuantity -= Multiplier;
			}

			return adjustedQuantity;
		}

		public void UpdateBlacklistStatus(List<string> blacklist)
		{
			Blacklisted = false;
			
			for (int i = 0; i < blacklist.Count; i++)
			{
				if (Description.Contains(blacklist[i]))
				{
					Blacklisted = true;
					break;
				}
			}
		}

		public void UpdatePriceExclusionStatus(float maximumUnitPrice, bool isEnabled) 
		{
			if (UnitPrice > maximumUnitPrice && isEnabled)
			{
				IsPriceExcluded = true;
			}
			else
			{
				IsPriceExcluded = false;
			}
		}
		
	}

}
