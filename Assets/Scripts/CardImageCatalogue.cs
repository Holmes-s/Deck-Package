using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

using ExitGames.Client.Photon;

namespace Photon.Pun.CardPackage
{
    public class CardImageCatalogue : MonoBehaviour
    {
        public List<string> BackImages;
        public List<string> FrontImageDictionaries;

        public Dropdown Back;
        public Dropdown Front;

        //TODO fix this to use a proper menu instead of current dropdown items

        public void SetStyleChoices()
        {
            Hashtable DeckChoice = new Hashtable() { { CardGame.DECK_ID, BackImages[Back.value] } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(DeckChoice);

            Hashtable CardChoice = new Hashtable() { { CardGame.CARD_ID, FrontImageDictionaries[Front.value] } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(CardChoice);
        }
    }
}