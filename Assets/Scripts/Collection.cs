using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.CardPackage
{
	public abstract class Collection : MonoBehaviour
	{
		public List<PlayingCard> cards;
		public List<PlayingCard> discards;

		/// <summary>
		/// Discard individual card to discard location based on index location
		/// </summary>
		/// <param name="index">target card</param>
		public abstract void Discard(int index);

		/// <summary>
		/// Discard entire collection to discard location
		/// </summary>
		public abstract void DiscardAll();

		/// <summary>
		/// randomize order of collection items
		/// </summary>
		public abstract void Shuffle();

		/// <summary>
		/// add a card item to the collection at the end of array
		/// </summary>
		/// <param name="card">Card to add</param>
		public abstract void Add(PlayingCard card);

		/// <summary>
		/// Add a list of cards to the collection at the end of array
		/// </summary>
		/// <param name="card">List of cards</param>
		/// <param name="index">location to add list</param>
		public abstract void Add(List<PlayingCard> card);

		/// <summary>
		/// add a card item to the collection at the location
		/// </summary>
		/// <param name="card">Card to add</param>
		/// <param name="index">location to add</param>
		public abstract void Add(PlayingCard card, int index = 0);

		/// <summary>
		/// Add a list of cards to the collection at the location
		/// </summary>
		/// <param name="card">List of cards</param>
		/// <param name="index">location to add list</param>
		public abstract void Add(List<PlayingCard> card, int index = 0);

		/// <summary>
		/// deletes the card at target index
		/// </summary>
		/// <param name="index">target location</param>
		public abstract void Remove(int index);
		/// <summary>
		/// deletes the card by Reference
		/// </summary>
		/// <param name="index">target location</param>
		public abstract void Remove(PlayingCard card);

		/// <summary>
		/// deletes all cards in the collection
		/// </summary>
		public abstract void Clear();

	}
}