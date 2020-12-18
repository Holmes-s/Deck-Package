using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Photon.Pun.CardPackage
{
    [RequireComponent(typeof(PhotonView))]
    public class CardView : MonoBehaviour, IPunObservable
    {
        private PlayingCard card;

        public Sprite FrontPic;
        public Sprite BackPic;

        private Vector3 DragStartPos;
        private Vector2Int DragStartParent;
        private bool dragLastOnlyViolation = false;
        private int DragStartChildIndex;
        private bool draggingThis = false;

        private bool allowStackDrag = false;
        private bool allowMaintainOffset = false;
        private List<GameObject> DragGroup = new List<GameObject>();

        public bool allowZoom;
        public DragRules dragRules;
        public bool isFaceUp = false;

        public PhotonView photonView;
        private bool CardSet = false;

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(draggingThis);
                stream.SendNext(allowStackDrag);
                stream.SendNext(allowMaintainOffset);
                stream.SendNext(allowZoom);
                stream.SendNext(dragRules);
                stream.SendNext(isFaceUp);
            }
            else
            {
                draggingThis = (bool)stream.ReceiveNext();
                allowStackDrag = (bool)stream.ReceiveNext();
                allowMaintainOffset = (bool)stream.ReceiveNext();
                allowZoom = (bool)stream.ReceiveNext();
                dragRules = (DragRules)stream.ReceiveNext();
                isFaceUp = (bool)stream.ReceiveNext();
            }
        }

        private void OnEnable()
        {
            photonView = GetComponent<PhotonView>();

            //only do this once ever per card
            if (!CardSet)
            {
                FindObjectOfType<CardGameController>().GenCardInitiate(this);
                CardSet = true;
            }
        }

        /// <summary>
        /// Set click interactions and images for card
        /// </summary>
        void Start()
        {
            EventTrigger trigger = GetComponent<EventTrigger>();
            EventTrigger.Entry entryClickCard = new EventTrigger.Entry();
            EventTrigger.Entry entryStartDrag = new EventTrigger.Entry();
            EventTrigger.Entry entryDragging = new EventTrigger.Entry();
            EventTrigger.Entry entryEndDrag = new EventTrigger.Entry();

            entryClickCard.eventID = EventTriggerType.PointerDown;
            entryStartDrag.eventID = EventTriggerType.BeginDrag;
            entryDragging.eventID = EventTriggerType.Drag;
            entryEndDrag.eventID = EventTriggerType.EndDrag;

            entryClickCard.callback.AddListener((data) => { OnClickCard((PointerEventData)data); });
            entryStartDrag.callback.AddListener((data) => { OnStartDrag((PointerEventData)data); });
            entryDragging.callback.AddListener((data) => { OnDragging((PointerEventData)data); });
            entryEndDrag.callback.AddListener((data) => { OnEndDrag((PointerEventData)data); });

            trigger.triggers.Add(entryClickCard);
            trigger.triggers.Add(entryStartDrag);
            trigger.triggers.Add(entryDragging);
            trigger.triggers.Add(entryEndDrag);

            /////////////////

            card = gameObject.GetComponent<PlayingCard>();

            //grab images from card Dictionaries
            FrontPic = FindObjectOfType<CardImageFrontDictionary>().FindImage(card.frontpicIndex);
            BackPic = FindObjectOfType<CardImageBack>().BackImage;

            if (card.Facing)
                gameObject.GetComponent<Image>().sprite = FrontPic;
            else
                gameObject.GetComponent<Image>().sprite = BackPic;
        }

        /// <summary>
        /// <para>Control card zoom when enabled</para>
        /// </summary>
        void Update()
        {
            //Zoom
            if (allowZoom && !draggingThis)
            {
                if (Vector2.Distance(Input.mousePosition, card.transform.position) <= card.gameObject.GetComponent<RectTransform>().rect.height / 2f)
                {
                    float ScaleValue = 1 - (Vector2.Distance(Input.mousePosition, card.transform.position) / (card.gameObject.GetComponent<RectTransform>().rect.height / 2f));
                    card.gameObject.GetComponent<RectTransform>().localScale = new Vector3(1 + ScaleValue, 1 + ScaleValue, 1);
                }
                else
                {
                    card.gameObject.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
                }
            }
            else
            {
                card.gameObject.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            }

            if (card != null)
            {
                if (card.Facing != isFaceUp)
                    card.Facing = isFaceUp;

                if (gameObject.GetComponent<Image>().sprite != FrontPic || gameObject.GetComponent<Image>().sprite != BackPic)
                    if (card.Facing)
                        gameObject.GetComponent<Image>().sprite = FrontPic;
                    else
                        gameObject.GetComponent<Image>().sprite = BackPic;
            }
        }

        private bool ValidSpotCheck()
        {
            bool temp = false;

            temp = FindObjectOfType<CardGameController>().CheckValidDropPoint();

            return temp;
        }

        private bool DragCheck()
        {
            if (dragRules == DragRules.NoDragging)
                return false;

            if (dragRules == DragRules.BothSides)
                return true;

            if (dragRules == DragRules.FaceUp && isFaceUp)
                return true;

            if (dragRules == DragRules.FaceDown && !isFaceUp)
                return true;

            return false;
        }

        public void SetDragState()
        {
            if (gameObject.transform.parent.transform.parent.GetComponent<HandView>() != null)
            {
                HandView handView = gameObject.transform.parent.transform.parent.GetComponent<HandView>();

                dragRules = handView.dragRules;
            }
            else if (gameObject.transform.parent.transform.parent.GetComponent<DeckView>() != null)
            {
                DeckView deckView = gameObject.transform.parent.transform.parent.GetComponent<DeckView>();

                dragRules = deckView.dragRules;
            }
        }

        [PunRPC]
        public void Flip(bool forced = false)
        {
            if (forced)
            {
                isFaceUp = !isFaceUp;
            }
            else
            {
                //if hand or deck
                if (gameObject.transform.parent.transform.parent.GetComponent<HandView>() != null)
                {
                    HandView handView = gameObject.transform.parent.transform.parent.GetComponent<HandView>();

                    //is last in hand
                    if (handView.onlyFlipLast)
                        if (gameObject.transform.GetSiblingIndex() != gameObject.transform.parent.transform.childCount - 1)
                            return;

                    if (handView.flipRules != FlipRules.NoFlipping)
                    {
                        if (handView.flipRules == FlipRules.FlipBothWays)
                        {
                            isFaceUp = !isFaceUp;
                        }
                        else if (handView.flipRules == FlipRules.FlipToUp && !isFaceUp)
                        {
                            isFaceUp = !isFaceUp;
                        }
                        else if (handView.flipRules == FlipRules.FlipToDown && isFaceUp)
                        {
                            isFaceUp = !isFaceUp;
                        }
                    }
                }
                else if (gameObject.transform.parent.transform.parent.GetComponent<DeckView>() != null)
                {
                    DeckView deckView = gameObject.transform.parent.transform.parent.GetComponent<DeckView>();

                    if (isFaceUp != deckView.facingUp)
                        isFaceUp = !isFaceUp;
                }
            }

            SetDragState();

            if (card != null)
            {
                card.Facing = isFaceUp;

                if (card.Facing)
                    gameObject.GetComponent<Image>().sprite = FrontPic;
                else
                    gameObject.GetComponent<Image>().sprite = BackPic;
            }
        }

        #region Dragging Triggers and Functions
        public void OnClickCard(PointerEventData data)
        {
            if (FindObjectOfType<CardGameController>().UseTurns)
                if (FindObjectOfType<CardGameController>().InteractTurnRestricted && FindObjectOfType<CardGameController>().ActivePlayerTurn != CardGameController.getActorIndex(PhotonNetwork.LocalPlayer.ActorNumber))
                    return;

            TriggerFlip();
        }
        public void OnStartDrag(PointerEventData data)
        {
            if (FindObjectOfType<CardGameController>().UseTurns)
                if (FindObjectOfType<CardGameController>().InteractTurnRestricted && FindObjectOfType<CardGameController>().ActivePlayerTurn != CardGameController.getActorIndex(PhotonNetwork.LocalPlayer.ActorNumber))
                    return;

            TriggerSaveDragStartPos();
        }
        public void OnDragging(PointerEventData data)
        {
            if (FindObjectOfType<CardGameController>().UseTurns)
                if (FindObjectOfType<CardGameController>().InteractTurnRestricted && FindObjectOfType<CardGameController>().ActivePlayerTurn != CardGameController.getActorIndex(PhotonNetwork.LocalPlayer.ActorNumber))
                    return;

            TriggerDragging();
        }
        public void OnEndDrag(PointerEventData data)
        {
            if (FindObjectOfType<CardGameController>().UseTurns)
                if (FindObjectOfType<CardGameController>().InteractTurnRestricted && FindObjectOfType<CardGameController>().ActivePlayerTurn != CardGameController.getActorIndex(PhotonNetwork.LocalPlayer.ActorNumber))
                    return;

            TriggerStopDragging();
        }

        /// <summary>
        /// Function for responding to clicking to flip
        /// </summary>
        public void TriggerFlip()
        {
            HandView handView = null;
            DeckView deckView = null;

            //if card from hand or deck
            if (gameObject.transform.parent.transform.parent.GetComponent<HandView>() != null)
                handView = gameObject.transform.parent.transform.parent.GetComponent<HandView>();
            else if (gameObject.transform.parent.transform.parent.GetComponent<DeckView>() != null)
                deckView = gameObject.transform.parent.transform.parent.GetComponent<DeckView>();

            if (handView != null)
            {
                if (handView.allowClickFlip)
                {
                    photonView.RPC("Flip", RpcTarget.All, false);
                }
            }
            else if (deckView != null)
            {
                if (deckView.flipRules != FlipRules.NoFlipping)
                    photonView.RPC("Flip", RpcTarget.All, false);
            }
        }

        /// <summary>
        /// <para>Check valid conditions for starting the drag functions.</para>
        /// <para>Save card current state so it can be returned if there is an invalid placement area</para>
        /// <para>If all stages are passed move to draggable state</para>
        /// </summary>
        public void TriggerSaveDragStartPos()
        {
            HandView handView = null;
            DeckView deckView = null;

            draggingThis = false;

            if (gameObject.transform.parent.transform.parent.GetComponent<HandView>() != null)
                handView = gameObject.transform.parent.transform.parent.GetComponent<HandView>();
            else if (gameObject.transform.parent.transform.parent.GetComponent<DeckView>() != null)
                deckView = gameObject.transform.parent.transform.parent.GetComponent<DeckView>();

            dragLastOnlyViolation = false;

            //if must be last card in hand
            if (handView != null)
                if (handView.onlyDragLastCard)
                {
                    int countActive = 0;
                    foreach (Transform item in gameObject.transform.parent.transform)
                        if (item.gameObject.activeSelf)
                            countActive += 1;

                    if (gameObject.transform.GetSiblingIndex() != countActive - 1)
                    {
                        dragLastOnlyViolation = true;
                        return;
                    }
                }

            if (DragCheck())
            {
                if (handView != null)
                {
                    allowStackDrag = handView.enableStackDrag;
                    allowMaintainOffset = handView.maintainOffset;
                    dragRules = handView.dragRules;
                }
                else if (deckView != null)
                {
                    dragRules = deckView.dragRules;
                    dragLastOnlyViolation = false;
                }

                //Save starting state
                photonView.RPC("SetDragStartPos", RpcTarget.All);
                SetDragStartParent(card.gameObject.transform.parent.gameObject.transform.parent.gameObject);
                DragStartChildIndex = card.gameObject.transform.GetSiblingIndex();

                //Start Dragging // todo did I destroy a group of brackets?
                if (allowStackDrag)
                {
                    for (int i = DragStartChildIndex + 1; i < card.gameObject.transform.parent.childCount; i++)
                    {
                        card.gameObject.transform.parent.GetChild(i).GetComponent<CardView>().DragStartChildIndex = i;
                        card.gameObject.transform.parent.GetChild(i).GetComponent<CardView>().photonView.RPC("SetDragStartPos", RpcTarget.All);
                        DragGroup.Add(card.gameObject.transform.parent.GetChild(i).gameObject);
                    }
                }

                //move to dragging canvas for the purpose of visual layering 
                photonView.RPC("SetToDragLayer", RpcTarget.All);

                if (allowStackDrag)
                    foreach (var item in DragGroup)
                        item.GetPhotonView().RPC("SetToDragLayer", RpcTarget.All);
            }
        }

        /// <summary>
        /// Update position of card
        /// </summary>
        public void TriggerDragging()
        {
            //if following only last card rule
            if (dragLastOnlyViolation)
                return;

            //if drag is valid
            if (DragCheck())
            {
                draggingThis = true;
                Vector3 frameStartLocation = card.gameObject.GetComponent<RectTransform>().position;

                card.gameObject.GetComponent<RectTransform>().position = Input.mousePosition;
                if (FindObjectOfType<CardGameController>().PhotonCardDragMovement)
                    photonView.RPC("SetDragPosition", RpcTarget.Others, Input.mousePosition);

                if (allowStackDrag)
                {
                    if (allowMaintainOffset)
                    {
                        foreach (var item in DragGroup)
                        {
                            Vector3 tempPos = item.GetComponent<RectTransform>().position;
                            Vector3 tempDistance = frameStartLocation - tempPos;

                            item.GetComponent<RectTransform>().position = Input.mousePosition - tempDistance;
                            if (FindObjectOfType<CardGameController>().PhotonCardDragMovement)
                                item.GetPhotonView().RPC("SetDragPosition", RpcTarget.All, Input.mousePosition - tempDistance);

                        }
                    }
                    else
                    {
                        foreach (var item in DragGroup)
                        {
                            item.GetComponent<RectTransform>().position = Input.mousePosition;
                            if (FindObjectOfType<CardGameController>().PhotonCardDragMovement)
                                item.GetPhotonView().RPC("SetDragPosition", RpcTarget.All, Input.mousePosition);

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Release the Card from drag effect and place in target location if valid or return to starting positions
        /// </summary>
        public void TriggerStopDragging()
        {
            //if following only last card rule
            if (dragLastOnlyViolation)
                return;

            HandView handView = null;
            DeckView deckView = null;

            if (GetDragTargetParent(DragStartParent).GetComponent<HandView>() != null)
                handView = GetDragTargetParent(DragStartParent).GetComponent<HandView>();
            else if (GetDragTargetParent(DragStartParent).GetComponent<DeckView>() != null)
                deckView = GetDragTargetParent(DragStartParent).GetComponent<DeckView>();


            if (DragCheck())
            {
                bool transfer = false;
                Vector3 targPos = Vector3.zero;
                bool validSpot = ValidSpotCheck();

                //if dropped in range of valid placemantArea
                if (validSpot)
                {
                    int ValidAreaId = FindObjectOfType<CardGameController>().ValidAreaId;

                    if (GetDragTargetParent(new Vector2Int(0, ValidAreaId)) == GetDragTargetParent(DragStartParent))
                        validSpot = false;
                }

                if (validSpot)
                {
                    int ValidAreaId = FindObjectOfType<CardGameController>().ValidAreaId;
                    PlacementArea pa = FindObjectOfType<CardGameController>().GCLists.TargetAreas[ValidAreaId];

                    if (handView != null)
                    {
                        //if stack and stack not allowed
                        if (DragGroup.Count > 0 && !pa.allowStackPlacement)
                        {
                            photonView.RPC("SetToParentZone", RpcTarget.All, DragStartParent.y, new Vector2(DragStartParent.x, DragStartParent.y), transfer, DragStartPos);

                            foreach (var item in DragGroup)
                            {
                                item.GetPhotonView().RPC("SetToParentZone", RpcTarget.All, DragStartParent.y, new Vector2(DragStartParent.x, DragStartParent.y), transfer, DragStartPos);
                            }
                            DragGroup.Clear();
                            allowStackDrag = false;
                            allowMaintainOffset = false;
                            draggingThis = false;
                            return;
                        }
                        else
                        {
                            transfer = pa.ConditionCheck(card);

                            if (transfer)
                            {
                                photonView.RPC("SetToParentZone", RpcTarget.All, ValidAreaId, new Vector2(DragStartParent.x, DragStartParent.y), transfer, DragStartPos);
                            }
                            else
                            {
                                targPos = DragStartPos;
                                photonView.RPC("SetToParentZone", RpcTarget.All, DragStartParent.y, new Vector2(DragStartParent.x, DragStartParent.y), transfer, DragStartPos);
                            }

                            if (!transfer && pa.stackStopOnFail)
                            {
                                foreach (var item in DragGroup)
                                {
                                    item.GetPhotonView().RPC("SetToParentZone", RpcTarget.All, DragStartParent.y, new Vector2(DragStartParent.x, DragStartParent.y), transfer, DragStartPos);
                                }
                            }
                            else
                            {
                                bool stackstop = false;

                                //check placement area rules for card stack
                                for (int i = 0; i <= DragGroup.Count - 1; i++)
                                {
                                    bool ConditionPassed = false;

                                    if (!stackstop)
                                        if (pa.stackIndividualRuleChecks)
                                            if (!pa.ConditionCheck(DragGroup[i].GetComponent<PlayingCard>()))
                                            {
                                                if (pa.stackStopOnFail)
                                                    stackstop = true;
                                            }
                                            else
                                                ConditionPassed = true;
                                        else
                                            ConditionPassed = true;

                                    if (ConditionPassed)
                                        DragGroup[i].GetPhotonView().RPC("SetToParentZone", RpcTarget.All, ValidAreaId, new Vector2(DragStartParent.x, DragStartParent.y), ConditionPassed, DragStartPos);
                                    else
                                        DragGroup[i].GetPhotonView().RPC("SetToParentZone", RpcTarget.All, DragStartParent.y, new Vector2(DragStartParent.x, DragStartParent.y), ConditionPassed, DragStartPos);
                                }
                            }

                            DragGroup.Clear();
                            allowStackDrag = false;
                            allowMaintainOffset = false;
                            draggingThis = false;
                            return;
                        }
                    }
                    else if (deckView != null)
                    {
                        transfer = pa.ConditionCheck(card);

                        if (!transfer)
                        {
                            targPos = DragStartPos;

                            photonView.RPC("SetToParentZone", RpcTarget.All, DragStartParent.y, new Vector2(DragStartParent.x, DragStartParent.y), transfer, DragStartPos);

                            foreach (var item in DragGroup)
                            {
                                item.GetPhotonView().RPC("SetToParentZone", RpcTarget.All, DragStartParent.y, new Vector2(DragStartParent.x, DragStartParent.y), transfer, DragStartPos);
                            }
                        }
                        else
                        {
                            photonView.RPC("SetToParentZone", RpcTarget.All, ValidAreaId, new Vector2(DragStartParent.x, DragStartParent.y), transfer, DragStartPos);

                            foreach (var item in DragGroup)
                            {
                                item.GetPhotonView().RPC("SetToParentZone", RpcTarget.All, ValidAreaId, new Vector2(DragStartParent.x, DragStartParent.y), transfer, DragStartPos);
                            }
                        }
                        DragGroup.Clear();
                        allowStackDrag = false;
                        allowMaintainOffset = false;
                        draggingThis = false;
                        return;
                    }
                }
                else
                {
                    //reset if not released in valid location
                    photonView.RPC("SetToParentZone", RpcTarget.All, DragStartParent.y, new Vector2(DragStartParent.x, DragStartParent.y), transfer, DragStartPos);

                    foreach (var item in DragGroup)
                    {
                        item.GetPhotonView().RPC("SetToParentZone", RpcTarget.All, DragStartParent.y, new Vector2(DragStartParent.x, DragStartParent.y), transfer, DragStartPos);
                    }

                    DragGroup.Clear();
                    allowStackDrag = false;
                    allowMaintainOffset = false;
                    draggingThis = false;
                }
            }
        }

        #region PunFunctions

        /// <summary>
        /// Needed to convert gameObjects into an information format that can be passed in Pun.RPC calls
        /// </summary>
        void SetDragStartParent(GameObject t)
        {
            int i;
            GameControllerValues lists = FindObjectOfType<CardGameController>().GCLists;

            if (lists.TargetAreas.Contains(t.GetComponent<PlacementArea>()))
            {
                i = lists.TargetAreas.IndexOf(t.GetComponent<PlacementArea>());

                DragStartParent = new Vector2Int(0, i);
                return;
            }
            if (lists.Hands.Contains(t.GetComponent<HandView>()))
            {
                i = lists.Hands.IndexOf(t.GetComponent<HandView>());

                DragStartParent = new Vector2Int(1, i);
                return;
            }
            if (lists.Decks.Contains(t.GetComponent<DeckView>()))
            {
                i = lists.Decks.IndexOf(t.GetComponent<DeckView>());

                DragStartParent = new Vector2Int(2, i);
                return;
            }
        }

        /// <summary>
        /// Needed to deconvert information
        /// </summary>
        Transform GetDragTargetParent(Vector2Int v2)
        {
            Transform startPoint = this.transform.parent;

            GameControllerValues lists = FindObjectOfType<CardGameController>().GCLists;

            switch (v2.x)
            {
                case 0:
                    startPoint = lists.TargetAreas[v2.y].transform;
                    break;
                case 1:
                    startPoint = lists.Hands[v2.y].transform;
                    break;
                case 2:
                    startPoint = lists.Decks[v2.y].transform;
                    break;
                default:
                    break;
            }

            return startPoint;
        }

        [PunRPC]
        void SetToParentZone(int TargetAreaNum, Vector2 DragStart, bool passed, Vector3 setPos)
        {
            Vector2Int startP = new Vector2Int((int)DragStart.x, (int)DragStart.y);

            if (passed)
            {
                GameControllerValues list = FindObjectOfType<CardGameController>().GCLists;
                list.TargetAreas[TargetAreaNum].AddCardToZone(card);

                if (GetDragTargetParent(startP).GetComponent<HandView>() != null)
                {
                    GetDragTargetParent(startP).GetComponent<HandView>().handObject.Remove(card);
                    GetDragTargetParent(startP).GetComponent<HandView>().UpdateCardDisplay();
                }
                else if (GetDragTargetParent(startP).GetComponent<DeckView>() != null)
                {
                    GetDragTargetParent(startP).GetComponent<DeckView>().DeckObject.Remove(card);
                    GetDragTargetParent(startP).GetComponent<DeckView>().UpdateCardDisplay();
                }
            }
            else
            {
                card.gameObject.GetComponent<RectTransform>().position = setPos;

                if (GetDragTargetParent(startP).GetComponent<HandView>() != null)
                {
                    card.gameObject.transform.SetParent(GetDragTargetParent(startP).GetComponent<HandView>().cardZone.transform);
                    card.gameObject.transform.SetSiblingIndex(DragStartChildIndex);
                    GetDragTargetParent(startP).GetComponent<HandView>().UpdateCardDisplay();
                }
                else if (GetDragTargetParent(startP).GetComponent<DeckView>() != null)
                {
                    card.gameObject.transform.SetParent(GetDragTargetParent(startP).GetComponent<DeckView>().DeckZone.transform);
                    GetDragTargetParent(startP).GetComponent<DeckView>().UpdateCardDisplay();
                }
            }
        }

        [PunRPC]
        public void SetDragStartPos()
        {
            DragStartPos = card.gameObject.GetComponent<RectTransform>().position;
        }

        [PunRPC]
        public void SetDragPosition(Vector3 targetPos)
        {
            //todo doesnt work right if screen sizes are different
            card.gameObject.GetComponent<RectTransform>().position = targetPos;
            card.gameObject.GetComponent<RectTransform>().localEulerAngles = Vector3.zero;
        }

        [PunRPC]
        public void SetToDragLayer()
        {
            card.gameObject.transform.SetParent(FindObjectOfType<CardGameController>().dragLayer);
        }

        #endregion

        #endregion
    }
}