using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KeySeller.Business
{
    public class Game : Entity
    {
        public string Name { get; set; }

        public double Price { get; set; }

        public bool Sold { get; set; }

        public HumbleChoice Choice { get; set; }

        public string VideoUrl { get; set; }

        public override string ToString()
        {
            return $"Name: {Name}\nPrice: €{Price}";
        }
    }
}
