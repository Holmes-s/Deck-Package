using UnityEngine;
using UnityEngine.UI;

namespace Photon.Pun.CardPackage
{
    public class ButtonControls : MonoBehaviour
    {
        public bool ShowOnlyOnHost = true;
        public bool OnlyOnActivePlayerTurn = true;
        public bool ActivateSelfOnActiveTurn = true;

        private CardGameController CGC;

        void Start()
        {
            CGC = FindObjectOfType<CardGameController>();

            if (ShowOnlyOnHost)
                if (PhotonNetwork.LocalPlayer.ActorNumber == PhotonNetwork.MasterClient.ActorNumber)
                    this.gameObject.SetActive(true);
                else
                    this.gameObject.SetActive(false);
        }

        void Update()
        {
            if (ActivateSelfOnActiveTurn)
                if (CGC.ActivePlayerTurn != CardGameController.getActorIndex(PhotonNetwork.LocalPlayer.ActorNumber))
                    this.gameObject.GetComponent<Button>().interactable = false;
                else
                    this.gameObject.GetComponent<Button>().interactable = true;
        }


        public void StartGameButton()
        {
            if (OnlyOnActivePlayerTurn)
                if (CGC.ActivePlayerTurn != CardGameController.getActorIndex(PhotonNetwork.LocalPlayer.ActorNumber))
                    return;

            CGC.SolitaireSetup();
        }

        public void EndTurnButton()
        {
            if (OnlyOnActivePlayerTurn)
                if (CGC.ActivePlayerTurn != CardGameController.getActorIndex(PhotonNetwork.LocalPlayer.ActorNumber))
                    return;

            CGC.pTurnManager.BeginTurn();
            this.gameObject.GetComponent<Button>().interactable = false;
        }

        public void DrawButton()
        {
            if (OnlyOnActivePlayerTurn)
                if (CGC.ActivePlayerTurn != CardGameController.getActorIndex(PhotonNetwork.LocalPlayer.ActorNumber))
                    return;

            CGC.DrawThroughPun();
        }

        public void ToggleActiveButton()
        {
            if (OnlyOnActivePlayerTurn)
                if (CGC.ActivePlayerTurn != CardGameController.getActorIndex(PhotonNetwork.LocalPlayer.ActorNumber))
                    return;

            this.gameObject.SetActive(!this.gameObject.activeSelf);
        }

        public void QuitToLobby()
        {
            PhotonNetwork.Disconnect();
            PhotonNetwork.JoinLobby();
        }
    }
}