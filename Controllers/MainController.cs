using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Controllers
{
	public class MainController
	{
		//for float conversion
		protected static CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();

		protected static bool actionIsCancelled = false;

		static MainController()
		{
			ci.NumberFormat.CurrencyDecimalSeparator = ".";
		}

		protected static float GetFloat(string text)
		{
			return float.Parse(text, NumberStyles.Any, ci);
		}

		protected static string GetFormattedTime(TimeSpan timespan)
		{
			string time = string.Empty;

			//Hours
			time += timespan.Hours > 9 ? timespan.Hours.ToString() : "0" + timespan.Hours.ToString() + ":";
			//Minutes
			time += timespan.Minutes > 9 ? timespan.Minutes.ToString() : "0" + timespan.Minutes.ToString() + ":";
			//Seconds
			time += timespan.Seconds > 9 ? timespan.Seconds.ToString() : "0" + timespan.Seconds.ToString();
			
			return time;
		}

		//For updating the display
		private static List<MyIObserver> observers = new List<MyIObserver>();

		public static void AddObserver(MyIObserver observer)
		{
			observers.Add(observer);
		}

		protected static void OverwriteDisplay(string message = "")
		{
			for (int i = 0; i < observers.Count; i++)
			{
				observers[i].OverwriteDisplay(message);
			}
		}

		protected static void AddToDisplay(string message)
		{
			for (int i = 0; i < observers.Count; i++)
			{
				observers[i].AddToDisplay(message);
			}
		}

		protected static void UpdateProgress(string percentage)
		{
			for (int i = 0; i < observers.Count; i++)
			{
				observers[i].UpdateProgressPercentage(percentage);
			}
		}

		protected static void UnlockUI()
		{
			for (int i = 0; i < observers.Count; i++)
			{
				observers[i].UnlockUI();
			}
		}

		public static void CancelAction()
		{
			actionIsCancelled = true;
		}
	}
}
