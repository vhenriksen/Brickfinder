using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model;
using System.Text.RegularExpressions;
using System.Net;
using System.Globalization;

namespace DataAccessLayer
{
	public class DAO
	{
		private static List<Brick> bricks = new List<Brick>();
		private static WantedList wantedList = new WantedList();
		private static List<Shop> shops = new List<Shop>();

		public static List<Brick> GetBricks()
		{
			return bricks;
		}

		public static Brick GetBrickByIOidentifier(string ioIdentifier)
		{
			Brick returnValue = null;
			for (int i = 0; i < bricks.Count; i++)
			{

				if (bricks[i].IOidentifier.Equals(ioIdentifier))
				{
					returnValue = bricks[i];
					break;
				}
			}

			return returnValue;
		}

		public static WantedList GetWantedList()
		{
			return wantedList;
		}

		public static List<Shop> GetShops()
		{
			return shops;
		}

		public static Shop GetShopByHashcode(int hashcode)
		{
			Shop returnValue = null;
			for (int i = 0; i < shops.Count; i++)
			{
				if (shops[i].GetHashCode() == hashcode)
				{
					returnValue = shops[i];
					break;
				}
			}

			return returnValue;
		}

		//Returns the brick that matches the attributes. If not found, create the brick and return it.
		public static Brick GetOrCreateBrick(string name, string id, int bricklinkColorId, float brickWeight = 0, int bricklinkId = -1, bool isMinifig = false)
		{
			Brick returnValue = null;

			//Look for an existing brick
			for (int i = 0; i < bricks.Count; i++)
			{
				if (
					bricks[i].Id.Equals(id)
					&& bricks[i].BricklinkColorId == bricklinkColorId
					&& bricks[i].Name.Equals(name)
					)
				{
					returnValue = bricks[i];
					break;
				}
			}

			//Create a new brick if not found
			if (returnValue == null)
			{
				if (brickWeight == 0 || bricklinkId < 0)
				{
					LookUpBrickweightAndId(id, out bricklinkId, out brickWeight, isMinifig);
				}
				
				returnValue = new Brick(
										name,
										id,
										bricklinkId,
										bricklinkColorId,
										brickWeight
										);

				bricks.Add(returnValue);
			}

			return returnValue;
		}

		//Returns the shop that matches the attributes. If not found create the shop and return it.
		public static Shop GetOrCreateShop(string name, string country, float minimumBuy)
		{
			Shop returnValue = null;

			//Look for an existing shop
			for (int i = 0; i < shops.Count; i++)
			{
				if (
					shops[i].Name.Equals(name)
					&& shops[i].Country.Equals(country)
					)
				{
					returnValue = shops[i];
					break;
				}
			}

			//Create a new shop if not found
			if (returnValue == null)
			{
				returnValue = new Shop(name, country, minimumBuy);
				shops.Add(returnValue);
			}

			return returnValue;
		}

		//Looks up this bricks weight and Id on bricklink
		private static Regex regexBrickWeight = new Regex(@"(?<=Weight \(in grams\):</FONT><BR>)\d+\.\d+", RegexOptions.Compiled);
		private static Regex regexBricklinkId = new Regex(@"(?<=search\.asp\?itemID=)\d+", RegexOptions.Compiled);
		private static CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone(); //for float conversion

		//private static float LookUpBrickweightAndId(string brickId, out string bricklinkId, out string brickWeight)
		private static void LookUpBrickweightAndId(string brickId, out int bricklinkId, out float brickWeight, bool isMinifig)
		{
			string localSourcecode;
			
			if (isMinifig)
			{
				localSourcecode = GetWebPage("http://www.bricklink.com/catalogItem.asp?M=" + brickId);
			}
			else
			{
				localSourcecode = GetWebPage("http://www.bricklink.com/catalogItem.asp?P=" + brickId);
			}

			//Find and convert the bricks weight
			string foundWeight = regexBrickWeight.Match(localSourcecode).ToString();

			//This can happen in rare circumstances
			if (foundWeight.Equals(""))
			{
				foundWeight = "0.5";
			}

			ci.NumberFormat.CurrencyDecimalSeparator = ".";
			brickWeight = float.Parse(foundWeight, NumberStyles.Any, ci);

			//Find and convert the bricklinkId
			string foundBricklinkId = regexBricklinkId.Match(localSourcecode).ToString();

			if (foundBricklinkId != string.Empty)
			{
				bricklinkId = Convert.ToInt32(foundBricklinkId);
			}
			else
			{
				throw new Exception("Bricklink ID not found for brick with ID: " + brickId.ToString());
			}
		}

		//If the brick is already in the wantedlist, adds to the quantity
		//if not creates a new entry with the quantity
		public static void AdjustWantedlist(Brick brick, int quantity, Condition condition)
		{
			wantedList.Add(brick, quantity, condition);
		}

		//All calls to the outside goes through here
		private static WebClient client = new WebClient();
		public static string GetWebPage(string url)
		{
			return client.DownloadString(url);
		}

		//Initiates a new shoplist
		public static void ClearShops()
		{
			shops.Clear();
		}

		//Used when loading data
		public static void ClearAllData()
		{
			ClearShops();
			wantedList = new WantedList();
			bricks.Clear();
		}
	}
}
