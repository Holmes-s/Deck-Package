using UnityEngine;
using UnityEngine.UI;

using ExitGames.Client.Photon;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;

namespace Photon.Pun.CardPackage
{
    public class PlayerListEntryCustom : MonoBehaviour
    {
        [Header("UI References")]
        public Text PlayerNameText;

        public Image PlayerColorImage;
        public Button PlayerReadyButton;
        public Image PlayerReadyImage;

        private int ownerId;
        private bool isPlayerReady;

        #region UNITY

        public void OnEnable()
        {
            PlayerNumbering.OnPlayerNumberingChanged += OnPlayerNumberingChanged;
        }

        public void Start()
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber != ownerId)
            {
                PlayerReadyButton.gameObject.SetActive(false);
            }
            else
            {
                //Hashtable initialProps = new Hashtable() { { CardGame.PLAYER_READY, isPlayerReady }, { CardGame.DECK_ID, 0 }, { CardGame.CARD_ID, 0 } };
                Hashtable initialProps = new Hashtable() { { CardGame.PLAYER_READY, isPlayerReady }, { CardGame.DECK_ID, 0 }, { CardGame.CARD_ID, 0 } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);
                PhotonNetwork.LocalPlayer.SetScore(0);

                PlayerReadyButton.onClick.AddListener(() =>
                {
                    isPlayerReady = !isPlayerReady;
                    SetPlayerReady(isPlayerReady);

                    Hashtable props = new Hashtable() { { CardGame.PLAYER_READY, isPlayerReady } };
                    PhotonNetwork.LocalPlayer.SetCustomProperties(props);

                    if (PhotonNetwork.IsMasterClient)
                    {
                        FindObjectOfType<LobbyMainPanelCustom>().LocalPlayerPropertiesUpdated();
                    }
                });
            }
        }

        public void OnDisable()
        {
            PlayerNumbering.OnPlayerNumberingChanged -= OnPlayerNumberingChanged;
        }

        #endregion

        public void Initialize(int playerId, string playerName)
        {
            ownerId = playerId;
            PlayerNameText.text = playerName;
        }

        private void OnPlayerNumberingChanged()
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == ownerId)
                {
                    //Todo This might be the place to set deckID and cardId in the lobby
                    //DeckID = number

                    //this should be removed but also replaced since I think it effects the colored square on the lobby
                    PlayerColorImage.color = Color.red;  // AsteroidsGame.GetColor(p.GetPlayerNumber());
                }
            }
        }

        public void SetPlayerReady(bool playerReady)
        {
            //Todo Rework this for proper card selection menu
            if (PhotonNetwork.LocalPlayer.ActorNumber == ownerId)
            {
                if (playerReady)
                    FindObjectOfType<CardImageCatalogue>().SetStyleChoices();
                FindObjectOfType<LobbyMainPanelCustom>().CardSelectPanel.SetActive(!playerReady);
            }

            PlayerReadyButton.GetComponentInChildren<Text>().text = playerReady ? "Ready!" : "Ready?";
            PlayerReadyImage.enabled = playerReady;
        }

        public void SetCardStyleChoice(bool Front, string Style)
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == ownerId)
                {
                    if (!Front)
                    {
                        Hashtable DeckChoice = new Hashtable() { { CardGame.DECK_ID, Style }};
                        PhotonNetwork.LocalPlayer.SetCustomProperties(DeckChoice);
                    }
                    else
                    {
                        Hashtable CardChoice = new Hashtable() { { CardGame.CARD_ID, Style } };
                        PhotonNetwork.LocalPlayer.SetCustomProperties(CardChoice);
                    }
                }
            }
        }

    }
}