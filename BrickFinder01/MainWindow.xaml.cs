using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using Controllers;
using System.Windows.Threading;

namespace BrickFinder01
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, MyIObserver
	{
		public MainWindow()
		{
			InitializeComponent();
			MainController.AddObserver(this);
		}

		private void ButtonExtractLots_Click(object sender, RoutedEventArgs e)
		{
			LockUI(true);
			string sourcecode = TextBoxWantedListSourcecode.Text;
			TextBoxWantedListSourcecode.Text = string.Empty;
			WantedListExtractorController.AsyncExtractWantedList(sourcecode);
		}

		private void ButtonQueryShops_Click(object sender, RoutedEventArgs e)
		{
			LockUI(true);
			bool isChecked = Convert.ToBoolean(CheckBoxLimitDataToEU.IsChecked);
			string shipCountryId = TextBoxShipCountryId.Text;
			QueryShopsController.AsyncQueryShops(isChecked, shipCountryId);
		}

		private void ButtonSaveData_Click(object sender, RoutedEventArgs e)
		{
			LockUI(true);
			TextBoxInfo.Text = "Saving data...";
			DataController.SaveData();
		}

		private void ButtonLoadData_Click(object sender, RoutedEventArgs e)
		{
			LockUI(true);
			TextBoxInfo.Text = "Loading data...";
			DataController.LoadData();
		}

		private void ButtonShowWantedList_Click(object sender, RoutedEventArgs e)
		{
			InformationController.ShowWantedList();
		}

		private void ButtonCombinationsToCheck_Click(object sender, RoutedEventArgs e)
		{
			int maxShops = Convert.ToInt32(TextBoxMaxShops.Text);
			int maxDeepth = Convert.ToInt32(TextBoxMaxDeepth.Text);

			long maxCombinations = maxShops;

			for (int i = 1; i < maxDeepth; i++)
			{
				maxCombinations *= maxShops;
			}

			MessageBox.Show("Maximum combinations to check: " + maxCombinations.ToString() + "\nTry to stay below 100.000 for fast results.");
		}

		private void ButtonFindPrice_Click(object sender, RoutedEventArgs e)
		{
			LockUI(true);
			float shipping = float.Parse(TextBoxShipping.Text);
			float currencyModifier = float.Parse(TextBoxCurrencyModifier.Text);
			int maxShops = Convert.ToInt32(TextBoxMaxShops.Text);
			int maxDeepth = Convert.ToInt32(TextBoxMaxDeepth.Text);
			float maxMedianPercentage = float.Parse(TextBoxMaxPercentage.Text);
			bool isMaxMedianPercentageEnabled = CheckBoxMaxPercentage.IsChecked ?? false;

			FindOfferController.AsyncFindOffer(currencyModifier, shipping, maxShops, maxDeepth, maxMedianPercentage, isMaxMedianPercentageEnabled);
		}

		private void ButtonCopyToOutput_Click(object sender, RoutedEventArgs e)
		{
			TextBoxWantedListSourcecode.Text = TextBoxInfo.Text;
		}

		//Observer stuff
		public void OverwriteDisplay(string message)
		{
			this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,

			(DispatcherOperationCallback)delegate(object arg)
			{
				// HANDLED ON UI THREAD
				this.TextBoxInfo.Text = message;
				return null;

			}, null);
		}

		public void AddToDisplay(string message)
		{
			this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,

			(DispatcherOperationCallback)delegate(object arg)
			{
				// HANDLED ON UI THREAD
				this.TextBoxInfo.Text = message + TextBoxInfo.Text;
				return null;

			}, null);
		}

		public void UpdateProgressPercentage(string percentage)
		{
			this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,

			(DispatcherOperationCallback)delegate(object arg)
			{
				// HANDLED ON UI THREAD
				this.Title = "BrickFinder 1.0 " + percentage;
				return null;

			}, null);
		}

		private void ButtonCancel_Click(object sender, RoutedEventArgs e)
		{
			MainController.CancelAction();
		}

		public void UnlockUI()
		{
			this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,

			(DispatcherOperationCallback)delegate(object arg)
			{
				// HANDLED ON UI THREAD
				LockUI(false);
				return null;

			}, null);
		}

		private void LockUI(bool @lock)
		{
			ButtonExtractLots.IsEnabled = !@lock;
			ButtonQueryShops.IsEnabled = !@lock;
			ButtonSaveData.IsEnabled = !@lock;
			ButtonLoadData.IsEnabled = !@lock;
			ButtonShowWantedList.IsEnabled = !@lock;
			ButtonCombinationsToCheck.IsEnabled = !@lock;
			ButtonFindPrice.IsEnabled = !@lock;
			ButtonCopyToOutput.IsEnabled = !@lock;
		}

	}
}