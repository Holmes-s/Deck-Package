using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.CardPackage
{
	public class Deck : Collection
	{
		public List<PlayingCard> savedCards;
		public int ID;

		public int backPicIndex;

		/// <summary>
		/// Save the card lists current state to return to it later
		/// </summary>
		public void SaveState()
		{
			savedCards = cards;
		}

		/// <summary>
		/// Return the card list to the original save state
		/// </summary>
		public void Reset()
		{
			cards = savedCards;
		}

		// ///////

		public override void Discard(int index)
		{
			discards.Add(cards[index]);

			Remove(index);
		}

		public override void DiscardAll()
		{
			discards.AddRange(cards);

			Clear();
		}

		[PunRPC]
		public override void Shuffle()
		{
			List<PlayingCard> temp = new List<PlayingCard>();
			int StartLength = cards.Count;

			for (int i = 0; i < StartLength; i++)
			{
				int tempRand = Random.Range(0, cards.Count);
				temp.Add(cards[tempRand]);
				cards.RemoveAt(tempRand);
			}
			cards.Clear();
			cards = temp;
		}

		public override void Add(PlayingCard card)
		{
			cards.Add(card);
		}

		public override void Add(List<PlayingCard> card)
		{
			cards.AddRange(card);
		}

		public override void Add(PlayingCard card, int index = 0)
		{
			cards.Insert(index, card);
		}

		public override void Add(List<PlayingCard> card, int index = 0)
		{
			cards.InsertRange(index, card);
		}

		public override void Remove(int index)
		{
			cards.RemoveAt(index);
		}

		public override void Remove(PlayingCard card)
		{
			cards.Remove(card);
		}

		public override void Clear()
		{
			cards.Clear();
		}
	}
}