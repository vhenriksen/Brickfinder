using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model;
using DataAccessLayer;

namespace Controllers
{
	public class InformationController : MainController
	{
		public static void ShowShopOverview()
		{
			StringBuilder sb = new StringBuilder();

			List<Shop> shops = DAO.GetShops();
			sb.Append(shops.Count.ToString() + " unique shops with at least 1 brick in stock\n\n");

			for (int i = 0; i < shops.Count; i++)
			{
				sb.Append(shops[i].ToString() + "\n");
			}

			OverwriteDisplay(sb.ToString());
		}

		public static void ShowWantedList()
		{
			OverwriteDisplay(DAO.GetWantedList().ToString());
		}

		//Not entiry sure what this is supposed to do. Leave for later.
		private void ShowShopLotsInfo() //ShowLotAvailibility or something
		{
			//StringBuilder sb = new StringBuilder();
			//List<Lot> missingLots = new List<Lot>();

			//foreach (Lot lot in wantedList)
			//{
			//    int offersForLot = shops.Select(x => x.GetLotOffer(lot)).Where(x => x.IsComplete).Count();

			//    if (offersForLot > 0)
			//    {
			//        sb.Append(offersForLot + " offers for lot: " + lot.ToString() + "\n");
			//    }
			//    else
			//    {
			//        missingLots.Add(lot);
			//    }
			//}

			//sb.Insert(0, "Total qualified offers for each of the following lots:\n\n");

			//if (missingLots.Count > 0)
			//{
			//    sb.Insert(0, "\n\n");

			//    foreach (Lot lot in missingLots) sb.Insert(0, "\n\n" + lot.ToString());

			//    sb.Insert(0, "No qualified offers found for the following lot" + (missingLots.Count > 1 ? "s:" : ":"));
			//}

			//LabelInfo.Content = sb.ToString();
		}

		//TODO: Not entirey sure how to handle NULL yet. Leave until the Shop class is in order
		public void ShowLowestShopPrices()
		{
			//StringBuilder sbQualified = new StringBuilder();
			//StringBuilder sbMissing = new StringBuilder();
			//StringBuilder sbIncomplete = new StringBuilder();
			//float totalPrice = 0;

			//List<Shop> shops = DAO.GetShops();
			//List<Lot> wantedList = DAO.GetWantedList().allLots;
			//for (int i = 0; i < wantedList.Count; i++)
			//{
			//    LotOffer bestCompleteLotOffer = null;
			//    LotOffer bestInCompleteLotOffer = null;

			//    for (int j = 0; j < shops.Count; j++)
			//    {
			//        LotOffer bestOfferCandidate = shops[j].GetLotOffer(wantedList[i]);
			//        if (bestOfferCandidate.IsComplete)
			//        {
			//            if (bestCompleteLotOffer != null)
			//            {
			//                if (bestOfferCandidate.LotPrice < bestCompleteLotOffer.LotPrice)
			//                {
			//                    bestCompleteLotOffer = bestOfferCandidate;
			//                }
			//            }
			//            else
			//            {
			//                bestCompleteLotOffer = bestOfferCandidate;
			//            }
			//        }
			//        else if (bestOfferCandidate.QuantityToBuy > 0)
			//        {
			//            if (bestInCompleteLotOffer != null)
			//            {
			//                if (bestOfferCandidate.QuantityToBuy > bestInCompleteLotOffer.QuantityToBuy)
			//                {
			//                    bestInCompleteLotOffer = bestOfferCandidate;
			//                }
			//            }
			//            else
			//            {
			//                bestInCompleteLotOffer = bestOfferCandidate;
			//            }
			//        }
			//    }

			//    if (bestCompleteLotOffer != null)
			//    {
			//        totalPrice += bestCompleteLotOffer.LotPrice;
			//        sbQualified.Append(bestCompleteLotOffer.ToString() + "\n" + bestCompleteLotOffer.FromShop.ToString() + "\n\n");
			//    }
			//    else if (bestInCompleteLotOffer != null)
			//    {
			//        totalPrice += bestInCompleteLotOffer.LotPrice;
			//        sbIncomplete.Append((bestInCompleteLotOffer.RequestedQuantity - bestInCompleteLotOffer.QuantityToBuy).ToString() + " missing from: " + bestInCompleteLotOffer.ToString() + "\n" + bestInCompleteLotOffer.FromShop.ToString() + "\n\n");

			//    }
			//    else
			//    {
			//        sbMissing.Append("All bricks missing from: " + wantedList[i].ToString() + "\n");
			//    }

			//}

			//OverwriteDisplay(
			//    "Total price: " + totalPrice.ToString("0.00") + " for the following lots:\n\n" +
			//    (sbMissing.Length < 1 ? string.Empty : "No offers found for the following lots:\n\n" + sbMissing.ToString() + "\n") +
			//    (sbIncomplete.Length < 1 ? string.Empty : "Missing bricks in the following lots:\n\n" + sbIncomplete.ToString() + "\n") +
			//    sbQualified.ToString()
			//    );
		}
	}
}
