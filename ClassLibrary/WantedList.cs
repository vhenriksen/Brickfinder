using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
	public class WantedList
	{
		public List<Lot> newLots = new List<Lot>();
		public List<Lot> nanLots = new List<Lot>();
		public List<Lot> allLots = new List<Lot>();

		public int LotCount()
		{
			int lotCount = 0;

			for (int i = 0; i < allLots.Count; i++)
			{
				if (allLots[i].WantedQuantity > 0) lotCount++;
			}

			return lotCount;
		}

		public List<Lot> GetUnmeetLots()
		{
			List<Lot> unmeetLots = new List<Lot>();

			for (int i = 0; i < allLots.Count; i++)
			{
				if (allLots[i].WantedQuantity > 0)
				{
					unmeetLots.Add(allLots[i]);
				}
			}

			return unmeetLots;
		}

		/// <summary>
		/// If the brick is already in the list, adds to the quantity
		/// if not creates a new entry with the quantity
		/// </summary>
		/// <param name="brick"></param>
		/// <param name="quantity"></param>
		/// <param name="condition"></param>
		public void Add(Brick brick, int quantity, Condition condition)
		{
			List<Lot> candidates = condition == Condition.New ? newLots : nanLots;
			Lot adjustLot = null;

			for (int i = 0; i < candidates.Count; i++)
			{
				if (candidates[i].Brick.Equals(brick))
				{
					adjustLot = candidates[i];
					break;
				}
			}

			if (adjustLot != null)
			{
				adjustLot.OriginalRequestedQuantity += quantity;
			}
			else
			{
				Lot lot = new Lot(brick, quantity, condition);
				candidates.Add(lot);
				allLots.Add(lot);
			}
		}

		public override string ToString()
		{
			StringBuilder info = new StringBuilder();

			int totalBricks = 0;

			for (int i = 0; i < allLots.Count; i++)
			{
				if (allLots[i].WantedQuantity > 0)
				{
					totalBricks += allLots[i].WantedQuantity;
					info.Append(allLots[i].ToString() + "\n");
				}
			}

			if (totalBricks > 0)
			{

				info.Insert(0, allLots.Count.ToString() + " unique lots, " + totalBricks.ToString() + " total bricks.\n\n");
			}
			else
			{
				info.Append("All lots fulfilled.\n\n");
			}

			return info.ToString();
		}
	}
}
