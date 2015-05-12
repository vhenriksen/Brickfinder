using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model;
using DataAccessLayer;
using System.IO;
using System.Threading;

namespace Controllers
{
	public class DataController : MainController
	{

		public static void SaveData()
		{
			Thread t1 = new Thread(SaveAllData);
			t1.IsBackground = true;
			t1.Start();
		}

		private static void SaveAllData()
		{
			DateTime startTime = DateTime.Now;
			StringBuilder data = new StringBuilder();

			List<Brick> bricks = DAO.GetBricks();
			for (int i = 0; i < bricks.Count; i++)
			{
				data.Append(string.Format("#0<{0}<{1}<{2}<{3}<{4}\r\n"
					, bricks[i].BricklinkId.ToString()
					, bricks[i].Id
					, bricks[i].BricklinkColorId.ToString()
					, bricks[i].Weight.ToString().Replace(",", ".")
					, bricks[i].Name.Replace("#", "")
					));
			}

			data.Append("\r\n");

			List<Lot> wantedList = DAO.GetWantedList().allLots;
			for (int i = 0; i < wantedList.Count; i++)
			{
				data.Append(string.Format("#1<{0}<{1}<{2}\r\n"
					, wantedList[i].Brick.IOidentifier
					, wantedList[i].WantedQuantity.ToString()
					, wantedList[i].Condition.ToString()
					));
			}

			data.Append("\r\n");

			List<Shop> shops = DAO.GetShops();
			for (int i = 0; i < shops.Count; i++)
			{
				data.Append(string.Format("#2<{0}<{1}<{2}\r\n\r\n"
					, shops[i].Name.Replace("\"", "\\\"").Replace("#", "")
					, shops[i].Country
					, shops[i].MinimumBuy.ToString().Replace(",", ".")
					));

				for (int j = 0; j < shops[i].AllInventory.Count; j++)
				{
					KeyValuePair<Brick, List<ShopItem>> brickShopItemList = shops[i].AllInventory.ElementAt(j);
					for (int s = 0; s < brickShopItemList.Value.Count; s++)
					{
						data.Append(string.Format("#3<{0}<{1}<{2}<{3}<{4}<{5}<{6}\r\n"
								, shops[i].GetHashCode().ToString()
								, brickShopItemList.Key.IOidentifier //The brick
								, brickShopItemList.Value[s].UnitPrice.ToString().Replace(",", ".")
								, brickShopItemList.Value[s].Condition.ToString()
								, brickShopItemList.Value[s].QuantityAvailible.ToString()
								, brickShopItemList.Value[s].Multiplier.ToString()
								, brickShopItemList.Value[s].Description.Replace("<","").Replace("#","")
								));
					}
				}
			}

			SaveDataToDisk(data.ToString());

			TimeSpan timePassed = DateTime.Now - startTime;
			OverwriteDisplay(
				"Data saved.\n" +
				bricks.Count.ToString() + " bricks, " +
				wantedList.Count.ToString() + " lots and " +
				shops.Count + " shops with inventory saved in:\n" +
				GetFormattedTime(timePassed)
				);

			UnlockUI();
		}

		private static void SaveDataToDisk(string data)
		{
			using (StreamWriter sw = new StreamWriter("Data.txt"))
			{
				//Overwrites previous data
				sw.Write(data);
			}
		}

		public static void LoadData()
		{
			Thread t1 = new Thread(LoadAllData);
			t1.IsBackground = true;
			t1.Start();
		}

		private static void LoadAllData()
		{
			DateTime startTime = DateTime.Now;
			DAO.ClearAllData();
			using (StreamReader sr = new StreamReader("Data.txt"))
			{
				string[] entries = sr.ReadToEnd().Split('#');

				foreach (string s in entries)
				{
					switch (s.Length > 0 ? s.ElementAt(0) : 'x')
					{
						case '0'://Brick
							RecreateBrickFromArray(s);
							break;
						case '1'://Lot
							RecreateLotFromArray(s);
							break;
						case '2'://Shop
							RecreateShopFromArray(s);
							break;
						case '3'://ShopItem
							RecreateShopItemFromArray(s);
							break;
						default:
							break;
					}
				}
			}

			TimeSpan timePassed = DateTime.Now - startTime;
			OverwriteDisplay(
				"Data loaded.\n" +
				DAO.GetBricks().Count.ToString() + " bricks, " +
				DAO.GetWantedList().allLots.Count.ToString() + " lots and " +
				DAO.GetShops().Count.ToString() + " shops with inventory loaded in " +
				GetFormattedTime(timePassed) + "."
				);

			UnlockUI();
		}

		private static void RecreateBrickFromArray(string s)
		{
			string[] att = s.Substring(2).Replace("\r\n", "").Split('<');
			int bricklinkId = Convert.ToInt32(att[0]);
			string brickId = att[1];
			int bricklinkColorId = Convert.ToInt32(att[2]);
			float brickWeight = GetFloat(att[3]);
			string brickName = att[4];

			DAO.GetOrCreateBrick(brickName, brickId, bricklinkColorId, brickWeight, bricklinkId);
		}

		private static void RecreateShopItemFromArray(string s)
		{
			string[] att = s.Substring(2).Replace("\r\n", "").Split('<');
			int shopHashCode = Convert.ToInt32(att[0]);
			string ioIdentifier = att[1];
			float price = GetFloat(att[2]);
			Condition condition = GetConditionFromString(att[3]);
			int quantity = Convert.ToInt32(att[4]);
			int multiplier = Convert.ToInt32(att[5]);
			string description = att[6];

			Shop shop = DAO.GetShopByHashcode(shopHashCode);
			Brick brick = DAO.GetBrickByIOidentifier(ioIdentifier);

			shop.CreateShopItem(brick, price, condition, quantity, multiplier, description);
		}

		private static void RecreateShopFromArray(string s)
		{
			string[] att = s.Substring(2).Replace("\r\n\r\n", "").Split('<');
			string shopName = att[0].Replace("\\\"", "\"");
			string country = att[1];
			float minimumBuy = GetFloat(att[2]);

			DAO.GetOrCreateShop(shopName, country, minimumBuy);
		}

		private static void RecreateLotFromArray(string s)
		{
			string[] att = s.Substring(2).Replace("\r\n", "").Split('<');
			string ioIdentifier = att[0];
			int quantity = Convert.ToInt32(att[1]);
			Condition condition = GetConditionFromString(att[2]);

			Brick brick = DAO.GetBrickByIOidentifier(ioIdentifier);

			DAO.AdjustWantedlist(brick, quantity, condition);
		}

		private static Condition GetConditionFromString(string condition)
		{
			if (condition.Equals("New")) return Condition.New;

			if (condition.Equals("Used")) return Condition.Used;

			else return Condition.NaN;
		}
	}
}
