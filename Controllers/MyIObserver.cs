using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Controllers
{
	public interface MyIObserver
	{
		void OverwriteDisplay(string message);

		void AddToDisplay(string message);

		void UpdateProgressPercentage(string percentage);

		void UnlockUI();
	}
}
