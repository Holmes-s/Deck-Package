using System.Collections;
using UnityEngine;

using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Hashtable = ExitGames.Client.Photon.Hashtable;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Photon.Pun.CardPackage
{
    public class CardGameController : MonoBehaviourPunCallbacks, IPunTurnManagerCallbacks, IPunObservable
    {
        public static CardGameController Instance = null;

        public DeckView deckView;
        public Transform dragLayer;
        public GameControllerValues GCLists;
        public string lobbySceneName;

        public bool UseTurns = false;
        public bool InteractTurnRestricted = false;

        public bool ShuffleToggle = true;
        public bool PhotonCardDragMovement = true;

        public int ValidAreaId { get; private set; }
        public bool GameActive { get; private set; } = false;
        public int CurrentTurn { get; private set; } = 0;
        public int PlayerCount { get; private set; } = 0;
        public int ActivePlayerTurn { get; private set; } = -1;

        private int GenCardCount = 0;
        private int genRange1 = (int)Suits.none;
        private int genRange2 = (int)Values.joker;

        private PhotonView pView;
        public PunTurnManager pTurnManager { get; private set; }
        private int MasterActorNumber = 0;

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (UseTurns)
            {
                if (stream.IsWriting)
                {
                    stream.SendNext(CurrentTurn);
                    stream.SendNext(ActivePlayerTurn);
                    stream.SendNext(GameActive);
                }
                else
                {
                    if (!PhotonNetwork.IsMasterClient)
                    {
                        CurrentTurn = (int)stream.ReceiveNext();
                        ActivePlayerTurn = (int)stream.ReceiveNext();
                        GameActive = (bool)stream.ReceiveNext();
                    }
                }
            }
        }

        #region UNITY
        public void Awake()
        {
            Instance = this;

            PhotonNetwork.AutomaticallySyncScene = true;

            pView = GetComponent<PhotonView>();
            pTurnManager = GetComponent<PunTurnManager>();

            object back;
            PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(CardGame.DECK_ID, out back);
            Instantiate(Resources.Load("CardBacks/" + (string)back), Vector3.zero, new Quaternion());
            object front;
            PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(CardGame.CARD_ID, out front);
            Instantiate(Resources.Load("CardFronts/" + (string)front), Vector3.zero, new Quaternion());
        }

        public void Start()
        {
            Hashtable props = new Hashtable
            {
                {CardGame.PLAYER_LOADED_LEVEL, true}
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            pTurnManager.TurnManagerListener = this;

            MasterActorNumber = PhotonNetwork.MasterClient.ActorNumber;
        }

        private void Update()
        {
            GameRules();
        }

        #endregion

        #region COROUTINES
        private IEnumerator EndOfGame()
        {
            float timer = 5.0f;

            while (timer > 0.0f)
            {
                yield return new WaitForEndOfFrame();

                timer -= Time.deltaTime;
            }

            PhotonNetwork.Disconnect();
            PhotonNetwork.JoinLobby();
        }

        #endregion

        #region PUN CALLBACKS

        public override void OnDisconnected(DisconnectCause cause)
        {
            if (ActivePlayerTurn == CardGameController.getActorIndex(PhotonNetwork.LocalPlayer.ActorNumber))
                pTurnManager.BeginTurn();

            UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.LogError("Player Left: " + otherPlayer.ActorNumber);

            if (ActivePlayerTurn == getActorIndex(otherPlayer.ActorNumber))
                if (PhotonNetwork.IsMasterClient)
                {
                    PlayerCount = PhotonNetwork.PlayerList.Length;
                    pTurnManager.BeginTurn();
                }
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            PlayerCount = PhotonNetwork.PlayerList.Length;

            if (UseTurns)
                if (getActorIndex(MasterActorNumber) == ActivePlayerTurn)
                    if (PhotonNetwork.IsMasterClient)
                    {
                        pTurnManager.BeginTurn();
                    }

            MasterActorNumber = PhotonNetwork.MasterClient.ActorNumber;
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps.ContainsKey(CardGame.PLAYER_LOADED_LEVEL))
            {
                if (CheckAllPlayerLoadedLevel())
                {
                    Debug.LogError("All Players Ready");

                    PlayerCount = PhotonNetwork.PlayerList.Length;
                }
                else
                {
                    // not all players loaded yet. wait:
                    Debug.Log("setting text waiting for players! ");
                }
            }
        }

        private bool CheckAllPlayerLoadedLevel()
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object playerLoadedLevel;

                if (p.CustomProperties.TryGetValue(CardGame.PLAYER_LOADED_LEVEL, out playerLoadedLevel))
                {
                    if ((bool)playerLoadedLevel)
                    {
                        continue;
                    }
                }

                return false;
            }

            return true;
        }
        #endregion

        #region Turns
        public void OnTurnBegins(int turn)
        {
            CurrentTurn = turn;
            ActivePlayerTurn = GetCurrentPlayerTurn();

            if (ActivePlayerTurn == getActorIndex(PhotonNetwork.LocalPlayer.ActorNumber))
                Debug.LogError("Turn Number: " + turn + " Your turn.");
        }

        public void OnTurnCompleted(int turn)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                pTurnManager.BeginTurn();
            }
        }

        public void OnPlayerMove(Player player, int turn, object move)
        {

        }

        public void OnPlayerFinished(Player player, int turn, object move)
        {

        }

        //cycle turns based on timer
        public void OnTurnTimeEnds(int turn)
        {
            //if (PhotonNetwork.IsMasterClient)
            //pTurnManager.BeginTurn();
        }

        public int GetCurrentPlayerTurn()
        {
            if (CurrentTurn == 0)
                return 0;

            int temp = CurrentTurn % PlayerCount;


            if (temp == 0)
                return PlayerCount;

            return temp;
        }

        public static int getActorIndex(int ActorNumber)
        {
            int index = 1;

            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == ActorNumber)
                    break;

                index += 1;
            }

            return index;
        }
        #endregion

        #region Game Setup
        /// <summary>
        /// Rename and rewrite for setup needs of the game you are making
        /// <para>For solitaire</para>
        /// <para>Make deck</para>
        /// <para>Place starter cards</para>
        /// </summary>
        public void SolitaireSetup()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                GenerateDeck();

                int n = 0;

                //draw face down cards
                for (int i = 4; i <= 10; i++)
                {
                    GCLists.TargetAreas[i].GetComponent<HandView>().photonView.RPC("DrawCard", RpcTarget.All, n);
                    n += 1;
                }

                //for solitaire - set rules for the 7 card zones
                for (int i = 4; i <= 10; i++)
                {
                    GCLists.TargetAreas[i].GetComponent<HandView>().flipRules = FlipRules.FlipToUp;
                    GCLists.TargetAreas[i].GetComponent<HandView>().autoFlipDrawn = true;
                    GCLists.TargetAreas[i].GetComponent<HandView>().onlyFlipLast = true;
                    GCLists.TargetAreas[i].GetComponent<HandView>().dragRules = DragRules.FaceUp;
                    GCLists.TargetAreas[i].GetComponent<HandView>().enableStackDrag = true;
                    GCLists.TargetAreas[i].GetComponent<HandView>().maintainOffset = true;

                    GCLists.TargetAreas[i].stackIndividualRuleChecks = false;
                    GCLists.TargetAreas[i].stackStopOnFail = false;

                    GCLists.TargetAreas[i].GetComponent<HandView>().photonView.RPC("DrawCard", RpcTarget.All, 1);

                    GCLists.TargetAreas[i].GetComponent<PlacementArea>().colorCondition = PlacementArea.ColorCondition.Opposite;
                    GCLists.TargetAreas[i].GetComponent<PlacementArea>().valueCondition = PlacementArea.ValueCondition.Descend;
                    GCLists.TargetAreas[i].GetComponent<PlacementArea>().allowStackPlacement = true;
                    GCLists.TargetAreas[i].GetComponent<PlacementArea>().stackStopOnFail = true;
                }

                GameActive = true;

                if (UseTurns)
                    pTurnManager.BeginTurn();
            }
        }

        /// <summary>
        /// Replace this with what is needed for the game you are making
        /// </summary>
        public void GenerateDeck()
        {
            //Generate standard Ace to King/Club to Spade amount of cards for deck
            for (int s = 0; s < genRange1; s++)
            {
                for (int v = 0; v < genRange2; v++)
                {
                    PhotonNetwork.InstantiateRoomObject("Card", Vector3.zero, new Quaternion());

                    GenCardCount += 1;
                }
            }
        }

        /// <summary>
        /// Cards will call this for themselves when first enabled
        /// set them into card values and suffles 
        /// syncing with other players 
        /// </summary>
        public void GenCardInitiate(CardView cv)
        {
            Vector3 DeckZonePos = deckView.DeckZone.GetComponent<RectTransform>().position;

            int c = 0;
            int sTemp = 0;
            int vTemp = 0;

            //Generate standard Ace to King/Club to Spade deck 
            for (int s = 0; s < genRange1; s++)
            {
                if (c > deckView.DeckObject.cards.Count)
                    break;
                for (int v = 0; v < genRange2; v++)
                {
                    sTemp = s;
                    vTemp = v;

                    c += 1;

                    if (c > deckView.DeckObject.cards.Count)
                        break;
                }
            }

            cv.name = "CardGameObject" + deckView.DeckObject.cards.Count;

            cv.gameObject.transform.SetParent(GCLists.Decks[0].DeckZone.transform);

            cv.gameObject.GetComponent<RectTransform>().position = new Vector3(DeckZonePos.x, DeckZonePos.y, 0);

            cv.gameObject.GetComponent<PlayingCard>().Set(sTemp, vTemp, false, 0, (sTemp * 13) + (vTemp), deckView.DeckID, cv.GetInstanceID());

            Vector2 DeckZoneSize = deckView.DeckZone.GetComponent<RectTransform>().sizeDelta;
            Vector2 newSize = new Vector2(0, 0);

            newSize.x = DeckZoneSize.x;
            newSize.y = DeckZoneSize.x * 1.4f;

            cv.gameObject.GetComponent<RectTransform>().sizeDelta = newSize;

            deckView.DeckObject.Add(cv.gameObject.GetComponent<PlayingCard>());


            if (deckView.DeckObject.cards.Count == (genRange1) * (genRange2))
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    foreach (var item in deckView.DeckObject.cards)
                        deckView.setCardStates(item);

                    if (ShuffleToggle)
                        deckView.DeckObject.Shuffle();

                    //for each card in deck run shuffle sync on all other players
                    for (int i = 0; i < deckView.DeckObject.cards.Count; i++)
                        photonView.RPC("ShuffleSync", RpcTarget.Others, i, deckView.DeckObject.cards[i].frontpicIndex);
                }
            }
        }

        /// <summary>
        /// sets given cards index to match the index locations in the hosts shuffled deck
        /// </summary>
        [PunRPC]
        void ShuffleSync(int index, int id)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                PlayingCard temp = deckView.DeckObject.cards[index];

                for (int i = 0; i < deckView.DeckObject.cards.Count; i++)
                {
                    if (deckView.DeckObject.cards[i].frontpicIndex == id)
                    {
                        deckView.DeckObject.cards[index] = deckView.DeckObject.cards[i];
                        deckView.DeckObject.cards[i] = temp;
                        return;
                    }
                }
            }
        }
        #endregion

        #region Game Controls
        /// <summary>
        /// Call the draw code from all players so they all see the same hand
        /// </summary>
        public void DrawThroughPun()
        {
            //Rename to match DrawCode
            photonView.RPC("SolitaireDrawCode", RpcTarget.All);
        }

        /// <summary>
        /// Rewrite for the game you are making and rename
        /// </summary>
        [PunRPC]
        private void SolitaireDrawCode()
        {
            foreach (var item in GCLists.Hands[0].handObject.cards)
            {
                item.GetComponent<CardView>().Flip(true);
            }

            DiscardAllFromHand();

            if (deckView.DeckObject.cards.Count != 0)
                GCLists.Hands[0].DrawCard(GCLists.Hands[0].cardLimit);
            else
                GCLists.Hands[0].RecycleDiscardsToDeck();
        }

        /// <summary>
        /// <para>Set placement zones to settings</para>
        /// <para>Check for win conditions</para>
        /// <para>And other stuff you want to add</para>
        /// </summary>
        public void GameRules()
        {
            if (GameActive)
            {
                SolitaireRules();

                CheckEndOfGame();
            }
        }

        /// <summary>
        /// <para>For solitaire</para>
        /// <para>Set rules for suit placement zones</para>
        /// <para>Set rules for play area placement zones</para>
        /// </summary>
        private void SolitaireRules()
        {
            for (int i = 0; i <= 3; i++)
            {
                if (GCLists.TargetAreas[i].GetComponent<DeckView>().DeckObject.cards.Count > 0)
                {
                    if (GCLists.TargetAreas[i].useSpecificRange == true && GCLists.TargetAreas[i].GetComponent<DeckView>().DeckObject.cards[0].Value == (int)Values.ace)
                    {
                        GCLists.TargetAreas[i].valueCondition = PlacementArea.ValueCondition.Ascend;
                        GCLists.TargetAreas[i].suitCondition = PlacementArea.SuitCondition.Match;
                        GCLists.TargetAreas[i].useSpecificRange = false;
                    }
                }
                else if (GCLists.TargetAreas[i].GetComponent<DeckView>().DeckObject.cards.Count == 0 && GCLists.TargetAreas[i].valueCondition == PlacementArea.ValueCondition.Ascend)
                {
                    GCLists.TargetAreas[i].valueCondition = PlacementArea.ValueCondition.Ignore;
                    GCLists.TargetAreas[i].suitCondition = PlacementArea.SuitCondition.Ignore;
                    GCLists.TargetAreas[i].useSpecificRange = true;
                }
            }

            for (int i = 4; i <= 10; i++)
            {
                if (GCLists.TargetAreas[i].GetComponent<HandView>().handObject.cards.Count == 0)
                {
                    GCLists.TargetAreas[i].GetComponent<PlacementArea>().colorCondition = PlacementArea.ColorCondition.Ignore;
                    GCLists.TargetAreas[i].GetComponent<PlacementArea>().valueCondition = PlacementArea.ValueCondition.Ignore;

                    GCLists.TargetAreas[i].GetComponent<PlacementArea>().useSpecificRange = true;
                    GCLists.TargetAreas[i].GetComponent<PlacementArea>().minMaxValue = new Vector2Int(12, 13);
                }
                else
                {
                    GCLists.TargetAreas[i].GetComponent<PlacementArea>().colorCondition = PlacementArea.ColorCondition.Opposite;
                    GCLists.TargetAreas[i].GetComponent<PlacementArea>().valueCondition = PlacementArea.ValueCondition.Descend;

                    GCLists.TargetAreas[i].GetComponent<PlacementArea>().useSpecificRange = false;
                    GCLists.TargetAreas[i].GetComponent<PlacementArea>().minMaxValue = new Vector2Int(0, 13);

                    //auto flip next card
                    if (GCLists.TargetAreas[i].GetComponent<HandView>().handObject.cards[GCLists.TargetAreas[i].GetComponent<HandView>().handObject.cards.Count - 1].Facing == false)
                    {
                        GCLists.TargetAreas[i].GetComponent<HandView>().handObject.cards[GCLists.TargetAreas[i].GetComponent<HandView>().handObject.cards.Count - 1].GetComponent<CardView>().photonView.RPC("Flip", RpcTarget.All, false);
                    }
                }
            }
        }

        /// <summary>
        /// <para>For solitaire.</para>
        /// <para>Check win states and return true</para>
        /// </summary>
        bool SolitaireWinCheck()
        {
            if (GCLists.TargetAreas[0].GetComponent<DeckView>().DeckObject.cards.Count == 13 &&
                GCLists.TargetAreas[1].GetComponent<DeckView>().DeckObject.cards.Count == 13 &&
                GCLists.TargetAreas[2].GetComponent<DeckView>().DeckObject.cards.Count == 13 &&
                GCLists.TargetAreas[3].GetComponent<DeckView>().DeckObject.cards.Count == 13)
            {
                return true;
            }

            if (GCLists.Hands[0].handObject.cards.Count == 0 &&
                GCLists.Hands[0].handObject.discards.Count == 0 &&
                deckView.DeckObject.cards.Count == 0)
            {
                bool allFaceUp = true;
                for (int i = 4; i <= 10; i++)
                {
                    for (int k = 0; k < GCLists.TargetAreas[i].GetComponent<HandView>().handObject.cards.Count - 1; k++)
                    {
                        if (GCLists.TargetAreas[i].GetComponent<HandView>().handObject.cards[k].Facing == false)
                        {
                            allFaceUp = false;
                            break;
                        }
                    }
                    if (!allFaceUp)
                        break;
                }

                if (allFaceUp)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// End Game if meeting Win Conditions
        /// </summary>
        private void CheckEndOfGame()
        {
            if (SolitaireWinCheck())
            {
                GameActive = false;

                if (PhotonNetwork.IsMasterClient)
                {
                    StopAllCoroutines();
                    StartCoroutine(EndOfGame());
                }
            }
        }
        #endregion

        #region Card Interaction Functions
        /// <summary>
        /// Checks if current mouse position is within the boundries of an area valid for card placement
        /// </summary>
        public bool CheckValidDropPoint()
        {
            bool temp = false;

            for (int i = 0; i < GCLists.TargetAreas.Count; i++)
            {
                Vector2 mousePos = Input.mousePosition;
                Vector3 targetBox = GCLists.TargetAreas[i].CardZone.transform.position;

                Vector3 targetBoxSize = RectTransformUtility.PixelAdjustRect(GCLists.TargetAreas[i].CardZone.GetComponent<RectTransform>(), GetComponentInParent<Canvas>()).size;

                targetBoxSize *= GetComponentInParent<Canvas>().scaleFactor;

                Vector3 ZoneMin = (new Vector3(targetBox.x - targetBoxSize.x / 2, targetBox.y - targetBoxSize.y / 2, 0));
                Vector3 ZoneMax = (new Vector3(targetBox.x + targetBoxSize.x / 2, targetBox.y + targetBoxSize.y / 2, 0));

                if (mousePos.x >= ZoneMin.x && mousePos.y >= ZoneMin.y && mousePos.x <= ZoneMax.x && mousePos.y <= ZoneMax.y)
                {
                    ValidAreaId = i;
                    temp = true;
                }
            }
            return temp;
        }

        /// <summary>
        /// Adds card to placement area if conditions are met
        /// </summary>
        /// <param name="card">Card input for check</param>
        /// <param name="handView">Parent hand area</param>
        /// <param name="useRules">If placement area rules should be acknowledged</param>
        public bool PlaceInPile(PlayingCard card, HandView handView, bool useRules)
        {
            if (useRules)
            {
                if (GCLists.TargetAreas[ValidAreaId].AddCardToZoneWithRuleCheck(card))
                {
                    handView.handObject.Remove(card);
                    handView.UpdateCardDisplay();
                    return true;
                }
            }
            else
            {
                GCLists.TargetAreas[ValidAreaId].AddCardToZone(card);
                handView.handObject.Remove(card);
                handView.UpdateCardDisplay();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds card to placement area if conditions are met
        /// </summary>
        /// <param name="card">Card input for check</param>
        /// <param name="deckView">Parent deck area</param>
        public bool PlaceInPile(PlayingCard card, DeckView deckView)
        {
            if (GCLists.TargetAreas[ValidAreaId].AddCardToZoneWithRuleCheck(card))
            {
                deckView.DeckObject.Remove(card);
                deckView.UpdateCardDisplay();
                return true;
            }
            return false;
        }

        /// <summary>
        /// for solitaire
        /// </summary>
        private void DiscardAllFromHand()
        {
            foreach (var item in GCLists.Hands[0].handObject.cards)
            {
                item.gameObject.SetActive(false);
            }

            GCLists.Hands[0].handObject.DiscardAll();
        }
        #endregion

        #region UNITY_EDITOR
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;

            for (int i = 0; i < GCLists.TargetAreas.Count; i++)
            {
                Vector2 mousePos = Input.mousePosition;
                Vector3 targetBox = GCLists.TargetAreas[i].CardZone.transform.position;

                Vector3 targetBoxSize = RectTransformUtility.PixelAdjustRect(GCLists.TargetAreas[i].CardZone.GetComponent<RectTransform>(), GetComponentInParent<Canvas>()).size;

                targetBoxSize *= GetComponentInParent<Canvas>().scaleFactor;

                Vector3 ZoneMin = (new Vector3(targetBox.x - targetBoxSize.x / 2, targetBox.y - targetBoxSize.y / 2, 0));
                Vector3 ZoneMax = (new Vector3(targetBox.x + targetBoxSize.x / 2, targetBox.y + targetBoxSize.y / 2, 0));

                Gizmos.DrawLine(
                        new Vector3(ZoneMin.x, ZoneMin.y),
                        new Vector3(ZoneMax.x, ZoneMax.y));

                Gizmos.DrawLine(
                        new Vector3(ZoneMin.x, ZoneMax.y),
                        new Vector3(ZoneMax.x, ZoneMin.y));
            }
        }
#endif
        #endregion
    }

    #region UNITY_EDITOR
#if UNITY_EDITOR
    [CustomEditor(typeof(CardGameController))]
    [CanEditMultipleObjects]
    public class GameControllerDisplay : Editor
    {
        #region Serialized
        SerializedProperty serDeckView;
        SerializedProperty serDragLayer;
        SerializedProperty serGCLists;
        SerializedProperty serLobbySceneName;
        SerializedProperty serUseTurns;
        SerializedProperty serInteractTurnRestricted;
        SerializedProperty serShuffleToggle;
        SerializedProperty serPhotonCardDragMovement;
        #endregion

        void OnEnable()
        {
            serDeckView = serializedObject.FindProperty("deckView");
            serDragLayer = serializedObject.FindProperty("dragLayer");
            serGCLists = serializedObject.FindProperty("GCLists");
            serLobbySceneName = serializedObject.FindProperty("lobbySceneName");
            serUseTurns = serializedObject.FindProperty("UseTurns");
            serInteractTurnRestricted = serializedObject.FindProperty("InteractTurnRestricted");
            serShuffleToggle = serializedObject.FindProperty("ShuffleToggle");
            serPhotonCardDragMovement = serializedObject.FindProperty("PhotonCardDragMovement");
        }

        override public void OnInspectorGUI()
        {
            serializedObject.Update();

            var thisScript = target as CardGameController;

            GUILayout.Label("Required");
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.ObjectField(serGCLists, new GUIContent("GameControllerLists"));
            EditorGUILayout.ObjectField(serDeckView, new GUIContent("DeckView"));
            EditorGUILayout.ObjectField(serDragLayer, new GUIContent("DragLayer"));
            serLobbySceneName.stringValue = EditorGUILayout.TextField("Lobby Scene Name", serLobbySceneName.stringValue);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Toggle Settings");
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            serUseTurns.boolValue = EditorGUILayout.Toggle("Use Turns", serUseTurns.boolValue);
            if (thisScript.UseTurns)
                serInteractTurnRestricted.boolValue = EditorGUILayout.Toggle("Active Turn Only", serShuffleToggle.boolValue);
            serPhotonCardDragMovement.boolValue = EditorGUILayout.Toggle("Sync Drag Position", serPhotonCardDragMovement.boolValue);

            serShuffleToggle.boolValue = EditorGUILayout.Toggle("Shuffle", serShuffleToggle.boolValue);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
    #endregion
}