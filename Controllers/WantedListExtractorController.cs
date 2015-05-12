using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using Model;
using DataAccessLayer;

namespace Controllers
{
	public class WantedListExtractorController : MainController
	{
		private static Regex regexLotExtractor = new Regex(@"P/(?<BricklinkColorId>\d+)/(?<BrickId>[\d\w]+).*?P=[^>]+>(?<BrickName>[^<]+).*?SELECTED>(?<BrickCondition>New|Used|N/A).*?""q\d+"".*?VALUE=""(?<WantedQuantity>\d+)", RegexOptions.Compiled);
		private static Regex regexMinifigExtractor = new Regex(@"catalogItem.asp.M=(?<MinifigId>.*?)"">(?<MinifigName>.*?)</A>.*?SELECTED>(?<BrickCondition>New|Used|N/A).*?""q\d+"".*?VALUE=""(?<WantedQuantity>\d+)", RegexOptions.Compiled);
		private static string SourceCode;
		private static StringBuilder infoText;

		public static void AsyncExtractWantedList(string sourcecode)
		{
			actionIsCancelled = false;
			SourceCode = sourcecode;
			infoText = new StringBuilder();
			new Thread(ExtractWantedList).Start();
		}

		private static void ExtractWantedList()
		{
			ExtractBricks();
			ExtractMinifigs();

			AddToDisplay("Extraction complete.\n\n");
			UnlockUI();
		}

		private static void ExtractMinifigs()
		{
			MatchCollection allMinifigs = regexMinifigExtractor.Matches(SourceCode);

			//Extract info for each of the minifigs found
			StringBuilder minifigsInfoText = new StringBuilder();

			infoText.Append(allMinifigs.Count + " minifigs found.");

			for (int i = 0; i < allMinifigs.Count; i++)
			{
				if (actionIsCancelled) { break; }

				string minifigName = allMinifigs[i].Groups["MinifigName"].ToString();
				string minifigId = allMinifigs[i].Groups["MinifigId"].ToString();

				int quantity = Convert.ToInt32(allMinifigs[i].Groups["WantedQuantity"].ToString());

				Condition condition;
				if (allMinifigs[i].Groups["BrickCondition"].ToString().Equals("New")) condition = Condition.New;
				else condition = Condition.NaN;

				minifigsInfoText.Insert(0,
					"Minifig " + (i + 1).ToString() + ": " + quantity.ToString() + "x" +
					(condition == Condition.New ? " (N) " : " ") + minifigName + "\n");

				OverwriteDisplay(
					infoText.ToString() +
					" Extraction progress... " + (double)(((i + 1) * 100) / allMinifigs.Count) + "%.\n\n" +
					minifigsInfoText.ToString()
					);

				Brick brick = DAO.GetOrCreateBrick(minifigName, minifigId, 0, isMinifig: true);
				//Brick brick = DAO.GetOrCreateMinifig(minifigName, minifigId);
				DAO.AdjustWantedlist(brick, quantity, condition);
			}

			infoText.Append(" Extraction progress...complete.\n\n" + minifigsInfoText.ToString());

			OverwriteDisplay(infoText.ToString());
		}

		private static void ExtractBricks()
		{
			MatchCollection allLots = regexLotExtractor.Matches(SourceCode);

			//Extract info for each of the lots found
			StringBuilder lotsInfoText = new StringBuilder();

			infoText.Append(allLots.Count + " lots found.");

			for (int i = 0; i < allLots.Count; i++)
			{
				if (actionIsCancelled) { break; }

				string brickName = allLots[i].Groups["BrickName"].ToString();
				string brickId = allLots[i].Groups["BrickId"].ToString();

				//Any color has id = 0, but there is no way to get that info from the wantedList page
				int bricklinkColorId = Convert.ToInt32(allLots[i].Groups["BricklinkColorId"].ToString());

				int quantity = Convert.ToInt32(allLots[i].Groups["WantedQuantity"].ToString());

				Condition condition;
				if (allLots[i].Groups["BrickCondition"].ToString().Equals("New")) condition = Condition.New;
				else condition = Condition.NaN;

				lotsInfoText.Insert(0,
					"Lot " + (i + 1).ToString() + ": " + quantity.ToString() + "x" +
					(condition == Condition.New ? " (N) " : " ") + brickName + "\n");

				OverwriteDisplay(
					infoText.ToString() +
					" Extraction progress... " + (double)(((i + 1) * 100) / allLots.Count) + "%.\n\n" +
					lotsInfoText.ToString()
					);

				Brick brick = DAO.GetOrCreateBrick(brickName, brickId, bricklinkColorId);
				DAO.AdjustWantedlist(brick, quantity, condition);
			}

			infoText.Append(" Extraction progress...complete.\n\n" + lotsInfoText.ToString());

			OverwriteDisplay(infoText.ToString());
		}
	}
}
