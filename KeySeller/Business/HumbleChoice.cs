namespace KeySeller.Business
{
    public class HumbleChoice
    {
        public string Name { get; set; }

        public int ChoicesLeft { get; set; }

        public bool RemoveChoice()
        {
            if (ChoicesLeft <= 0)
            {
                ChoicesLeft--;
                return true;
            }
            else return false;
        }

        public override string ToString()
        {
            return $"Humble Choice {Name}\nAmount of choices left: {ChoicesLeft}";
        }
    }
}
