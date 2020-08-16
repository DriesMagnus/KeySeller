using MongoDB.Bson.Serialization.Attributes;

namespace KeySeller.Business
{
    public class Entity
    {
        /// <summary>
        /// The ID of given entity
        /// </summary>
        [BsonId] // _id
        public int Id { get; set; }
    }
}