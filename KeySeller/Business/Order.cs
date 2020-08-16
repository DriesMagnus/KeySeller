using System;
using System.Collections.Generic;
using NBitcoin;

namespace KeySeller.Business
{
    public class Order : Entity
    {
        /// <summary>
        /// The transaction ID of the BTC transaction.
        /// </summary>
        public string TransactionId { get; set; }

        /// <summary>
        /// Checks whether an order is finished (customer received the product).
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// The customer of the order.
        /// </summary>
        public Customer Customer { get; set; }

        /// <summary>
        /// How many confirmations the BTC transaction has in the blockchain.
        /// </summary>
        public int Confirmations { get; set; }

        /// <summary>
        /// The games that have been added to the cart.
        /// </summary>
        public List<Game> Games { get; set; }

        /// <summary>
        /// Whether or not the BTC transaction has been confirmed (more than 3 confirmations).
        /// </summary>
        public bool Confirmed { get; set; }

        /// <summary>
        /// The date the order had been placed.
        /// </summary>
        public DateTime Date { get; set; }
    }
}