namespace Model
{
	public class Lot
	{
		public Brick Brick { get; private set; }
		public int OriginalRequestedQuantity { get; set; }
		public int ReservedQuantity { get; set; } //ONLY for SHOPS to mess with, when a simulated purchase is made.
		public int WantedQuantity { get { return OriginalRequestedQuantity - ReservedQuantity; } }
		public Condition Condition { get; private set; }

		public Lot(Brick brick, int requestedQuantity, Condition condition)
		{
			Brick = brick;
			OriginalRequestedQuantity = requestedQuantity;
			ReservedQuantity = 0;
			Condition = condition;
		}

		public override string ToString()
		{
			return WantedQuantity.ToString() + "x" + (Condition == Condition.New ? " (N) " : " (NaN) ") + Brick.ToString();
		}
	}
}