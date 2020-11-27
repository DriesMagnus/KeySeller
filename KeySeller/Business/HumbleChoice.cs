namespace KeySeller.Business
{
    public class HumbleChoice
    {
        public string Name { get; set; }

        public int ChoicesLeft { get; set; }

        public override string ToString()
        {
            return $"{Name}\nAmount of choices left: {ChoicesLeft}";
        }
    }
}
