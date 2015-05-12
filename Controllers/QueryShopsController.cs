using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DataAccessLayer;
using Model;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Controllers
{
	public class QueryShopsController : MainController
	{
		//private static Regex regexOfferExtractor = new Regex(@"<B>(?<Condition>(Used|New))</B>.*?Loc:\s(?<Country>[^,]+),\sMin\sBuy:\s(None|~?DKK\s(?<MinimumBuy>\d+\.\d+))([^>]*>){5}(?<ShopName>[^<]+).*?Qty:[^<]*<B>(?<Quantity>\d+(\.\d+)?)(</B>)?(&nbsp;\(x(?<Multiplier>\d+(\.\d+)?)\))?.*?DKK\s(?<Price>\d+\.\d+)", RegexOptions.Compiled);
		private static Regex regexOfferExtractor = new Regex(@"<TR.*?CLASS=""tm"">.*?(?<Condition>(Used|New)).*?<TD><B>.*?</B>(<BR>(?<Description>.*?))?<FONT CLASS=""fv"">.*?Loc: (?<Country>(\s|\w)+).*?(?<MinBuy>((\d+,)?\d+\.\d+)?)</FONT>.*?store.*?>(?<ShopName>.*?)</A>.*?Qty.*?<B>(?<Quantity>.*?)</B>(&nbsp;\(x(?<Multiplier>.*?)\))?<BR>Each.*?(?<Price>(\d+,)?\d+\.\d+)", RegexOptions.Compiled);
		private static Regex regexPageInfo = new Regex(@"<B>(?<OffersFound>\d+\,?\d*)</B>[\s\w\.]+<B>(?<CurrentPage>\d+)</B>[\s\w]+<B>(?<TotalPages>\d+)</B>", RegexOptions.Compiled);
		private static Regex regexNextPage = new Regex(@"(?<=<a href=\"")[^>]*(?=\"">Next</a>)", RegexOptions.Compiled);

		private static bool IsEuropeOnly { get; set; }
		private static string ShipCountryId { get; set; }

		//private static IDictionary<string, int> words;

		public static void AsyncQueryShops(bool isEuropeOnly, string shipCountryId)
		{
			actionIsCancelled = false;
			IsEuropeOnly = isEuropeOnly;
			ShipCountryId = shipCountryId;
			new Thread(QueryShops).Start();
		}

		private static void QueryShops()
		{
			DAO.ClearShops(); //Remove the current shops
			//Get intial offerpage for each lot
			string lotsProgressText = string.Empty;
			StringBuilder infoTextForThisLot = new StringBuilder();
			DateTime startTime = DateTime.Now;
			List<Brick> brickList = DAO.GetBricks();
			for (int i = 0; i < brickList.Count; i++)
			{
				lotsProgressText =
					"Quering shops for offers on " + brickList.Count.ToString() + " lots\n" +
					"Total progress: " + (double)(((i + 1) * 100) / brickList.Count) + "%...\n\n";
				infoTextForThisLot.Insert(0, "Quering brick " + (i + 1).ToString() + ": " + brickList[i].ToString() + "\n\n");

				UpdateProgress((double)(((i + 1) * 100) / brickList.Count) + "%");
				OverwriteDisplay(lotsProgressText + infoTextForThisLot);

				string startAdress = string.Format("http://www.bricklink.com/search.asp?itemID={0}&colorID={1}&shipCountryID={2}&pmtMethodID=11&excBind=Y&excComplete=Y&searchSort=P&sz=500"
								, brickList[i].BricklinkId
								, brickList[i].BricklinkColorId
								, ShipCountryId
								);

				if (IsEuropeOnly) startAdress += "&sellerLoc=R&regionID=-1";


				if (!actionIsCancelled)
				{
					//Start recursive offer extractor
					ExtractOffersAndGotoNextPage(DAO.GetWebPage(startAdress), brickList[i], ref infoTextForThisLot, lotsProgressText);
				}
			}

			TimeSpan timePassed = DateTime.Now - startTime;
			if (actionIsCancelled)
			{
				DAO.ClearShops();
				OverwriteDisplay("Query cancelled.");
			}
			else
			{
				OverwriteDisplay(
					"Query complete.\n" +
					brickList.Count.ToString() + " lots queried in " +
					(timePassed.Hours > 9 ? timePassed.Hours.ToString() : "0" + timePassed.Hours.ToString()) + ":" +
					(timePassed.Minutes > 9 ? timePassed.Minutes.ToString() : "0" + timePassed.Minutes.ToString()) + ":" +
					(timePassed.Seconds > 9 ? timePassed.Seconds.ToString() : "0" + timePassed.Seconds.ToString()) + "\n\n" + infoTextForThisLot
					);
			}
			UpdateProgress("");
			UnlockUI();
		}

		private static void ExtractOffersAndGotoNextPage(string currentWebPage, Brick brick, ref StringBuilder infoTextForLot, string totalProgress)
		{
			Match pageInfo = regexPageInfo.Match(currentWebPage);

			if (!pageInfo.Success) //No items where found, terminate search
			{
				infoTextForLot.Insert(0, "0 shops found, extraction complete\n");

				return; 
			}

			int currentPage = Convert.ToInt32(pageInfo.Groups["CurrentPage"].ToString().Replace(",", ""));
			int totalPages = Convert.ToInt32(pageInfo.Groups["TotalPages"].ToString());
			string offersFound = pageInfo.Groups["OffersFound"].ToString().Replace(",", ".");

			OverwriteDisplay(
				totalProgress +
				offersFound +
				" shops found, extracting page " +
				currentPage.ToString() +
				" of " +
				totalPages.ToString() +
				" (" +
				(double)((currentPage * 100) / totalPages) +
				"%)\n" +
				infoTextForLot.ToString()
				);

			//Find all offers on the current page and extract them
			ExtractOffersOnWebPage(currentWebPage, brick);

			//Look for a link to a next page
			string nextWebPage = regexNextPage.Match(currentWebPage).ToString();

			//If found continue loop
			if (nextWebPage != string.Empty)
			{
				ExtractOffersAndGotoNextPage(DAO.GetWebPage("http://www.bricklink.com" + nextWebPage), brick, ref infoTextForLot, totalProgress);
			}
			else
			{//Add the offerinfo to the label 
				infoTextForLot.Insert(0, offersFound + " shops found, extraction complete\n");
			}
		}

		private static void ExtractOffersOnWebPage(string sourceText, Brick brick)
		{
			string lastName = string.Empty;
			string lastCountry = string.Empty;
			float lastMinimumBuy = float.MaxValue;
			float lastPrice = 0;
			int lastQuantity = 0;
			int lastMultiplier = int.MaxValue;
			Condition lastCondition = Condition.NaN;
			string lastDescription = "1234ThisTextIsUnique";

			foreach (Match match in regexOfferExtractor.Matches(sourceText))
			{
				//The shop
				string name = match.Groups["ShopName"].ToString().Trim().ToLower();
				string country = match.Groups["Country"].ToString();
				float minimumBuy = match.Groups["MinBuy"].ToString().Length > 0 ? GetFloat(match.Groups["MinBuy"].ToString().Replace(",", "")) : 0;

				//The offer
				float price = GetFloat(match.Groups["Price"].ToString().Replace(",", ""));

				int quantity = int.Parse(match.Groups["Quantity"].ToString().Replace(",", ""));
				int multiplier = match.Groups["Multiplier"].ToString().Length > 0 ? int.Parse(match.Groups["Multiplier"].ToString().Replace(",", "")) : 1;
				Condition condition = match.Groups["Condition"].ToString() == "New" ? Condition.New : Condition.Used;

				//The description, later to check for unwanted words
				string description = match.Groups["Description"].ToString().Trim().ToLower();

				//Only add this lot, if it isnt a repeat of the previous one (bug in BL)
				if (
					price != lastPrice ||
					!description.Equals(lastDescription) ||
					!name.Equals(lastName) ||
					!country.Equals(lastCountry) ||
					minimumBuy != lastMinimumBuy ||
					quantity != lastQuantity ||
					multiplier != lastMultiplier ||
					condition != lastCondition
					)
				{
					//Get the shop if exists, otherwise add a new one
					Shop shop = DAO.GetOrCreateShop(name, country, minimumBuy);

					//Save the collected info. 
					shop.CreateShopItem(brick, price, condition, quantity, multiplier, description);

					lastName = name;
					lastCountry = country;
					lastDescription = description;

					lastCondition = condition;

					lastMinimumBuy = minimumBuy;
					lastMultiplier = multiplier;
					lastPrice = price;
					lastQuantity = quantity;
				}
			}
		}

	}
}
