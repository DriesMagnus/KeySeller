using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace KeySeller.Business
{
    public class Customer
    {
        /// <summary>
        /// The Discord UserID of the customer.
        /// Should be converted to ulong/UInt64.
        /// </summary>
        public string UserId { get; set; }
    }
}