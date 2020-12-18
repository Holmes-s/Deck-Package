using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.CardPackage
{
	public class Hand : Collection
	{
		/// <summary>
		/// Draw an amount of cards from the beginning of a targeted card collection 
		/// </summary>
		/// <param name="collection">target card collection</param>
		/// <param name="amount">amount to draw</param>
		public int Draw(Collection collection, int amount = 1)
		{
			int Overflow = 0;

			if (amount >= collection.cards.Count)
			{
				Overflow = amount - collection.cards.Count;
				amount = collection.cards.Count;
			}

			Add(collection.cards.GetRange(0, amount));

			for (int i = 0; i < amount; i++)
			{
				collection.Remove(0);
			}

			//pull extra
			while (Overflow > 0 && collection.discards.Count != 0)
			{
				//return discards to deck if deck is empty
				if (collection.cards.Count == 0)
				{
					collection.Add(collection.discards);
					collection.discards.Clear();
					collection.Shuffle();
				}

				Add(collection.cards.GetRange(0, Overflow));

				int DeckCount = collection.cards.Count;

				for (int i = 0; i < DeckCount; i++)
				{
					Overflow -= 1;
					collection.Remove(0);
				}
			}

			if (collection.cards.Count == 0 && collection.discards.Count != 0)
			{
				//return discards to deck if deck is empty
				if (collection.cards.Count == 0)
				{
					collection.Add(collection.discards);
					collection.discards.Clear();
					collection.Shuffle();
				}
			}

			return Overflow;
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