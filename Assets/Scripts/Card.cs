using UnityEngine;

namespace Photon.Pun.CardPackage
{
	public abstract class Card : MonoBehaviour
	{
		/// <summary>
		/// <para>true == face side up</para>
		/// <para>false == face side down</para>
		/// </summary>
		public bool Facing;

		public int deckId;
		public int cardId;

		public int frontpicIndex;
		public int backpicIndex;
	}
}
