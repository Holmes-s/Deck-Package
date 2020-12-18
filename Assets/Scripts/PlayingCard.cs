using UnityEngine;

namespace Photon.Pun.CardPackage
{
    [RequireComponent(typeof(PhotonView))]
    public class PlayingCard : Card, IPunObservable
    {
        public int Suit = 0;
        public int Value = 0;

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(Suit);
                stream.SendNext(Value);
                stream.SendNext(Facing);
                stream.SendNext(deckId);
                stream.SendNext(cardId);
                stream.SendNext(frontpicIndex);
                stream.SendNext(backpicIndex);
            }
            else
            {
                Suit = (int)stream.ReceiveNext();
                Value = (int)stream.ReceiveNext();
                Facing = (bool)stream.ReceiveNext();
                deckId = (int)stream.ReceiveNext();
                cardId = (int)stream.ReceiveNext();
                frontpicIndex = (int)stream.ReceiveNext();
                backpicIndex = (int)stream.ReceiveNext();
            }
        }

        public void Set(int SuitInput, int ValueInput, bool FacingDefault, int backPic, int frontPic, int DeckId, int CardID)
        {
            Suit = SuitInput;
            Value = ValueInput;

            Facing = FacingDefault;

            backpicIndex = backPic;
            frontpicIndex = frontPic;

            deckId = DeckId;
            cardId = CardID;
        }
    }
}