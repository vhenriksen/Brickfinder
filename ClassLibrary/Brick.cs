namespace Model
{
	public class Brick
	{
		public string Name { get; private set; }
		public string Id { get; private set; }
		public int BricklinkId { get; private set; }
		public int BricklinkColorId { get; private set; }
		public float Weight { get; private set; }

		//Used for loading and saving data
		public string IOidentifier { get { return Id + "@" + BricklinkColorId; } }

		public Brick(string name, string id, int bricklinkId, int bricklinkColorId, float weight)
		{
			Name = name;
			Id = id;
			BricklinkId = bricklinkId;
			BricklinkColorId = bricklinkColorId;
			Weight = weight;
		}

		public override string ToString()
		{
			return "ID: " +Id + ", " + Name;
		}
	}

}
