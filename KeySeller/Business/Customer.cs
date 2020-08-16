using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace KeySeller.Business
{
    public class Customer
    {
        public ulong UserId { get; set; }
    }
}