using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Photon.Pun.CardPackage
{
    [RequireComponent(typeof(PhotonView))]
    public class HandView : MonoBehaviour, IPunObservable
    {
        public DeckView targetDeckController;
        private Deck targetDeck;

        public Hand handObject;
        public DisplayTypes displayType = DisplayTypes.Horizontal;
        public AlignDirections alignDirection = AlignDirections.Center;
        public VerticalDirections verticalDirection = VerticalDirections.Down;
        public LayeringDirection layeringDirection = LayeringDirection.Top;

        public bool customSpacing = false;
        public float maxSpacing;
        public bool customScaling = false;
        public float maxScale;

        public float arcCenterDistance = 1;
        public bool allowZoom;
        public FlipRules flipRules = FlipRules.FlipBothWays;
        public bool onlyFlipLast = false;
        public bool autoFlipDrawn = true;
        public bool allowClickFlip = false;
        public DragRules dragRules = DragRules.NoDragging;
        public bool onlyDragLastCard = false;
        public bool enableStackDrag;
        public bool maintainOffset = true;

        public bool cardLimitToggle = false;
        public int cardLimit = 3;


        public GameObject cardZone;
        public List<GameObject> cardObjects = new List<GameObject>();

        private Vector2 cardZoneScale = new Vector2();
        private Vector2 cardNewSize = new Vector2();

        public PhotonView photonView;

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(displayType);
                stream.SendNext(alignDirection);
                stream.SendNext(verticalDirection);
                stream.SendNext(layeringDirection);
                stream.SendNext(customSpacing);
                stream.SendNext(maxSpacing);
                stream.SendNext(customScaling);
                stream.SendNext(maxScale);
                stream.SendNext(arcCenterDistance);
                stream.SendNext(allowZoom);
                stream.SendNext(flipRules);
                stream.SendNext(onlyFlipLast);
                stream.SendNext(autoFlipDrawn);
                stream.SendNext(allowClickFlip);
                stream.SendNext(dragRules);
                stream.SendNext(onlyDragLastCard);
                stream.SendNext(enableStackDrag);
                stream.SendNext(maintainOffset);
                stream.SendNext(cardLimitToggle);
                stream.SendNext(cardLimit);
            }
            else
            {
                if (!PhotonNetwork.IsMasterClient)
                {
                    displayType = (DisplayTypes)stream.ReceiveNext();
                    alignDirection = (AlignDirections)stream.ReceiveNext();
                    verticalDirection = (VerticalDirections)stream.ReceiveNext();
                    layeringDirection = (LayeringDirection)stream.ReceiveNext();

                    customSpacing = (bool)stream.ReceiveNext();
                    maxSpacing = (float)stream.ReceiveNext();
                    customScaling = (bool)stream.ReceiveNext();
                    maxScale = (float)stream.ReceiveNext();
                    arcCenterDistance = (float)stream.ReceiveNext();
                    allowZoom = (bool)stream.ReceiveNext();

                    flipRules = (FlipRules)stream.ReceiveNext();
                    onlyFlipLast = (bool)stream.ReceiveNext();
                    autoFlipDrawn = (bool)stream.ReceiveNext();
                    allowClickFlip = (bool)stream.ReceiveNext();

                    dragRules = (DragRules)stream.ReceiveNext();
                    onlyDragLastCard = (bool)stream.ReceiveNext();
                    enableStackDrag = (bool)stream.ReceiveNext();
                    maintainOffset = (bool)stream.ReceiveNext();

                    cardLimitToggle = (bool)stream.ReceiveNext();
                    cardLimit = (int)stream.ReceiveNext();
                }
            }
        }

        private void OnEnable()
        {
            photonView = GetComponent<PhotonView>();
        }

        void setDisplay()
        {
            switch (displayType)
            {
                case DisplayTypes.Horizontal:
                    break;
                case DisplayTypes.Vertical:
                    break;
                case DisplayTypes.Arc:
                    break;
                default:
                    break;
            }
        }

        void Start()
        {
            targetDeck = targetDeckController.GetComponent<DeckView>().DeckObject;

            cardZoneScale = cardZone.GetComponent<RectTransform>().rect.size;
        }

        void Update()
        {
            cardZoneScale = cardZone.GetComponent<RectTransform>().rect.size;

            if (transform.childCount > 1)
            {
                Vector3 cardzonePos = cardZone.GetComponent<RectTransform>().position;

                //auto set parent and scale
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    if (transform.GetChild(i).GetComponent<PlayingCard>() != null)
                    {
                        GameObject temp = transform.GetChild(i).gameObject;

                        if (cardZoneScale.x > cardZoneScale.y)
                        {
                            cardNewSize.y = cardZoneScale.y;
                            cardNewSize.x = cardNewSize.y / 1.4f;

                        }
                        else
                        {
                            cardNewSize.x = cardZoneScale.x;
                            cardNewSize.y = cardZoneScale.x * 1.4f;
                        }

                        if (customScaling)
                            cardNewSize = cardNewSize * maxScale;

                        temp.GetComponent<RectTransform>().sizeDelta = cardNewSize;
                        temp.GetComponent<RectTransform>().position = new Vector3(cardzonePos.x, cardzonePos.y, 0);

                        cardObjects.Add(temp);

                        temp.transform.SetParent(cardZone.transform);
                        cardZone.transform.GetChild(0).transform.SetAsLastSibling();
                    }
                }

                UpdateCardDisplay();
            }
        }

        [PunRPC]
        public void UpdateCardDisplay()
        {
            int CardLayer = 0;
            int CardCount = handObject.cards.Count;
            Vector2 CardSpacing = new Vector2();
            Vector2 newXY = new Vector2();

            //position cards

            if (displayType == DisplayTypes.Horizontal)
            {
                for (int i = 0; i < CardCount; i++)
                {
                    newXY.x = 0;
                    newXY.y = 0;

                    if (customSpacing)
                    {
                        CardSpacing.x = 0 + (cardNewSize.x * maxSpacing);
                    }
                    else
                    {
                        if (cardZoneScale.x - cardNewSize.x <= cardNewSize.x * CardCount)
                            CardSpacing.x = (cardZoneScale.x - cardNewSize.x) / CardCount;
                        else
                            CardSpacing.x = cardNewSize.x;
                    }

                    //Linear Fan
                    if (alignDirection == AlignDirections.Center)
                    {
                        if (CardCount % 2 == 0)
                        {
                            newXY.x = i * CardSpacing.x - (Mathf.Floor(0.5f * CardCount) * CardSpacing.x - (CardSpacing.x / 2));
                        }
                        else
                        {
                            newXY.x = i * CardSpacing.x - (Mathf.Floor(0.5f * CardCount) * CardSpacing.x);
                        }
                    }

                    if (alignDirection == AlignDirections.Left)
                        newXY.x = i * CardSpacing.x - (cardZoneScale.x / 2) + (cardNewSize.x / 2);

                    if (alignDirection == AlignDirections.Right)
                        newXY.x = (CardCount - i - 1) * -CardSpacing.x + (cardZoneScale.x / 2) - (cardNewSize.x / 2);

                    handObject.cards[i].gameObject.GetComponent<RectTransform>().localPosition = new Vector3(newXY.x, newXY.y, CardLayer);
                    handObject.cards[i].gameObject.GetComponent<RectTransform>().localEulerAngles = Vector3.zero;
                }
            }

            if (displayType == DisplayTypes.Arc)
            {
                Vector3 CardZoneCenter = Vector3.zero;

                Debug.Log(cardZoneScale.y);

                Vector3 ArcPoint = (CardZoneCenter - new Vector3(0, (cardZoneScale.y / 2), 0)) * arcCenterDistance;
                float ArcCircleRadius = Vector3.Distance(CardZoneCenter + new Vector3(0, (cardZoneScale.y * 0.25f), 0), ArcPoint);
                float SpreadHalfAngle = Mathf.Acos((ArcCircleRadius - (cardZoneScale.y * 0.25f)) / ArcCircleRadius) * Mathf.Rad2Deg;

                Debug.Log(CardZoneCenter + " " + ArcPoint + " " + ArcCircleRadius + " " + SpreadHalfAngle);

                for (int i = 0; i < CardCount; i++)
                {
                    newXY.x = 0;
                    newXY.y = 0;

                    float spread = (SpreadHalfAngle * 2) / (CardCount - 1);
                    float a = (float)(90 - (Mathf.Abs((SpreadHalfAngle) - (spread * i)) / 2));
                    float b = (float)(90 - a);

                    float distance = 2 * ArcCircleRadius * Mathf.Cos(Mathf.Deg2Rad * a);

                    float horiOffset = distance * Mathf.Cos(Mathf.Deg2Rad * b);
                    float vertOffset = distance * Mathf.Sin(Mathf.Deg2Rad * b);

                    newXY.x = CardZoneCenter.x - horiOffset;
                    newXY.y = CardZoneCenter.y + (cardZoneScale.y * 0.25f) - vertOffset;

                    float chord = 2 * Mathf.Sqrt(Mathf.Pow(ArcCircleRadius, 2) - Mathf.Pow(ArcCircleRadius - (cardZoneScale.y * 0.25f), 2));

                    //Horizontal spread squisher, for when cards dont fill cardzone width
                    if (CardCount * (cardNewSize.x / 2) < chord)
                        newXY.x = newXY.x * (CardCount * (cardNewSize.x / 2) / chord);

                    //right half
                    if (i > (CardCount - 1) / 2)
                        newXY.x = -newXY.x;

                    handObject.cards[i].gameObject.GetComponent<RectTransform>().localPosition = new Vector3(newXY.x, newXY.y, CardLayer);
                    handObject.cards[i].gameObject.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, (SpreadHalfAngle) - (spread * i));
                }
            }

            if (displayType == DisplayTypes.Vertical)
            {
                if (verticalDirection == VerticalDirections.Down)
                {
                    for (int i = 0; i < CardCount; i++)
                    {
                        newXY.x = 0;
                        newXY.y = 0;

                        if (customSpacing)
                        {
                            CardSpacing.y = 0 + (cardNewSize.y * maxSpacing);
                        }
                        else
                        {
                            if (cardZoneScale.y - cardNewSize.y / 2 <= cardNewSize.y / 2 * CardCount)
                                CardSpacing.y = (cardZoneScale.y - cardNewSize.y) / CardCount;
                            else
                                CardSpacing.y = cardNewSize.y / 2;
                        }

                        newXY.y = i * -CardSpacing.y + (cardZoneScale.y / 2) - (cardNewSize.y / 2);

                        handObject.cards[i].gameObject.GetComponent<RectTransform>().localPosition = new Vector3(newXY.x, newXY.y, CardLayer);
                        handObject.cards[i].gameObject.GetComponent<RectTransform>().localEulerAngles = Vector3.zero;
                    }
                }

                if (verticalDirection == VerticalDirections.Up)
                {
                    for (int i = 0; i < CardCount; i++)
                    {
                        newXY.x = 0;
                        newXY.y = 0;

                        if (customSpacing)
                        {
                            CardSpacing.y = 0 + (cardNewSize.y * maxSpacing);
                        }
                        else
                        {
                            if (cardZoneScale.y - cardNewSize.y / 2 <= cardNewSize.y / 2 * CardCount)
                                CardSpacing.y = (cardZoneScale.y - cardNewSize.y) / CardCount;
                            else
                                CardSpacing.y = cardNewSize.y / 2;
                        }

                        newXY.y = (CardCount - i - 1) * CardSpacing.y - (cardZoneScale.y / 2) + (cardNewSize.y / 2);

                        handObject.cards[i].gameObject.GetComponent<RectTransform>().localPosition = new Vector3(newXY.x, newXY.y, CardLayer);
                        handObject.cards[i].gameObject.GetComponent<RectTransform>().localEulerAngles = Vector3.zero;
                    }
                }
            }

            SortChildren(layeringDirection);
        }

        private void SortChildren(LayeringDirection dir)
        {
            if (dir == LayeringDirection.Top)
            {
                for (int i = handObject.cards.Count - 1; i >= 0; i--)
                {
                    handObject.cards[i].gameObject.transform.SetAsFirstSibling();
                }
            }

            if (dir == LayeringDirection.Bottom)
            {
                for (int i = 0; i < handObject.cards.Count; i++)
                {
                    handObject.cards[i].gameObject.transform.SetAsFirstSibling();
                }
            }
        }
        [PunRPC]
        public void DrawCard(int drawAmount = 1)
        {
            //todo make some auto recycle function based on overlimit
            int overlimit;

            if (cardLimitToggle)
            {
                if (handObject.cards.Count + drawAmount > cardLimit)
                {
                    overlimit = drawAmount + handObject.cards.Count - cardLimit;

                    drawAmount = cardLimit - handObject.cards.Count;
                }
            }

            int HandSize = handObject.cards.Count;
            int Undrawable = handObject.Draw(targetDeck, drawAmount);

            for (int i = 0; i < drawAmount - Undrawable; i++)
            {
                PlayingCard temp = handObject.cards[HandSize + i];

                PlayingCard[] allCards = targetDeckController.GetComponentsInChildren<PlayingCard>();

                for (int j = 0; j < allCards.Length; j++)
                {
                    if (temp.Suit == allCards[j].Suit && temp.Value == allCards[j].Value)
                    {
                        allCards[j].transform.SetParent(transform);
                        break;
                    }
                }

                setCardStates(temp);

                if (autoFlipDrawn)
                    temp.GetComponent<CardView>().Flip(true);
            }

            if (Undrawable >= 1 && targetDeckController.recycle)
            {
                RecycleDiscardsToDeck();
            }

            //print drawn values
            /*
            string print = "";
            foreach (var item in HandObject.cards)
                print += Enum.GetName(typeof(Suits), item.Suit) + ", " + Enum.GetName(typeof(Values), item.Value) + " | ";
            Debug.Log(print);
            */
        }

        [PunRPC]
        public void RecycleDiscardsToDeck()
        {
            foreach (var item in handObject.discards)
            {
                item.gameObject.transform.SetParent(targetDeckController.DeckZone.transform);
            }

            targetDeck.Add(handObject.discards);

            foreach (var item in targetDeck.cards)
            {
                item.gameObject.SetActive(true);
            }

            handObject.discards.Clear();
        }

        private void setCardStates(PlayingCard card)
        {
            card.gameObject.GetComponent<CardView>().dragRules = dragRules;

            card.gameObject.GetComponent<CardView>().allowZoom = allowZoom;
        }

        #region UNITY_EDITOR
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;

            float screenScale = GetComponentInParent<Canvas>().scaleFactor;
            Vector3 cardZoneSize = RectTransformUtility.PixelAdjustRect(cardZone.GetComponent<RectTransform>(), GetComponentInParent<Canvas>()).size * screenScale;

            switch (displayType)
            {
                case DisplayTypes.Horizontal:
                    Gizmos.DrawLine(
                        new Vector3(cardZone.transform.position.x - (cardZoneSize.x / 2), cardZone.transform.position.y, -50),
                        new Vector3(cardZone.transform.position.x + (cardZoneSize.x / 2), cardZone.transform.position.y, -50));
                    break;
                case DisplayTypes.Vertical:
                    Gizmos.DrawLine(
                        new Vector3(cardZone.transform.position.x, cardZone.transform.position.y - (cardZoneSize.y / 2), -50),
                        new Vector3(cardZone.transform.position.x, cardZone.transform.position.y + (cardZoneSize.y / 2), -50));
                    break;
                case DisplayTypes.Arc:
                    Gizmos.DrawLine(
                        new Vector3(cardZone.transform.position.x - (cardZoneSize.x / 2), cardZone.transform.position.y, -50),
                        new Vector3(cardZone.transform.position.x + (cardZoneSize.x / 2), cardZone.transform.position.y, -50));
                    Gizmos.DrawLine(
                        new Vector3(cardZone.transform.position.x - (cardZoneSize.x / 8), cardZone.transform.position.y + (cardZoneSize.y / 4), -50),
                        new Vector3(cardZone.transform.position.x + (cardZoneSize.x / 8), cardZone.transform.position.y + (cardZoneSize.y / 4), -50));
                    break;
                default:
                    break;
            }
        }
#endif
        #endregion
    }

    #region UNITY_EDITOR
#if UNITY_EDITOR
    [CustomEditor(typeof(HandView))]
    [CanEditMultipleObjects]
    public class HandViewEditorDisplay : Editor
    {
        #region Serialized
        SerializedProperty serHandObject;
        SerializedProperty serTargetDeckController;
        SerializedProperty serCardZone;
        SerializedProperty serDisplayType;
        SerializedProperty serAlignDirection;
        SerializedProperty serLayeringDirection;
        SerializedProperty serArcCenterDistance;
        SerializedProperty serVerticalDirection;
        SerializedProperty serCustomSpacing;
        SerializedProperty serMaxSpacing;
        SerializedProperty serCustomScaling;
        SerializedProperty serMaxScale;
        SerializedProperty serCardLimitToggle;
        SerializedProperty serCardLimit;
        SerializedProperty serAllowZoom;
        SerializedProperty serFlipRules;
        SerializedProperty serAllowClickFlip;
        SerializedProperty serOnlyFlipLast;
        SerializedProperty serAutoFlipDrawn;
        SerializedProperty serDragRules;
        SerializedProperty serOnlyDragLastCard;
        SerializedProperty serEnableStackDrag;
        SerializedProperty serMaintainOffset;
        #endregion

        void OnEnable()
        {
            serHandObject = serializedObject.FindProperty("handObject");
            serTargetDeckController = serializedObject.FindProperty("targetDeckController");
            serCardZone = serializedObject.FindProperty("cardZone");
            serDisplayType = serializedObject.FindProperty("displayType");
            serAlignDirection = serializedObject.FindProperty("alignDirection");
            serLayeringDirection = serializedObject.FindProperty("layeringDirection");
            serArcCenterDistance = serializedObject.FindProperty("arcCenterDistance");
            serVerticalDirection = serializedObject.FindProperty("verticalDirection");
            serCustomSpacing = serializedObject.FindProperty("customSpacing");
            serMaxSpacing = serializedObject.FindProperty("maxSpacing");
            serCustomScaling = serializedObject.FindProperty("customScaling");
            serMaxScale = serializedObject.FindProperty("maxScale");
            serCardLimitToggle = serializedObject.FindProperty("cardLimitToggle");
            serCardLimit = serializedObject.FindProperty("cardLimit");
            serAllowZoom = serializedObject.FindProperty("allowZoom");
            serFlipRules = serializedObject.FindProperty("flipRules");
            serAllowClickFlip = serializedObject.FindProperty("allowClickFlip");
            serOnlyFlipLast = serializedObject.FindProperty("onlyFlipLast");
            serAutoFlipDrawn = serializedObject.FindProperty("autoFlipDrawn");
            serDragRules = serializedObject.FindProperty("dragRules");
            serOnlyDragLastCard = serializedObject.FindProperty("onlyDragLastCard");
            serEnableStackDrag = serializedObject.FindProperty("enableStackDrag");
            serMaintainOffset = serializedObject.FindProperty("maintainOffset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var thisScript = target as HandView;

            EditorGUILayout.ObjectField(serHandObject, new GUIContent("HandScript"));
            EditorGUILayout.ObjectField(serTargetDeckController, new GUIContent("Target DeckController"));
            EditorGUILayout.ObjectField(serCardZone, new GUIContent("Card Zone"));

            EditorGUILayout.PropertyField(serDisplayType);

            if (thisScript.displayType == DisplayTypes.Horizontal)
            {
                EditorGUILayout.PropertyField(serAlignDirection);
                EditorGUILayout.PropertyField(serLayeringDirection);
            }
            else if (thisScript.displayType == DisplayTypes.Arc)
            {
                serArcCenterDistance.floatValue = EditorGUILayout.FloatField("Arc Center Distance", serArcCenterDistance.floatValue);
                EditorGUILayout.PropertyField(serLayeringDirection);
            }
            else if (thisScript.displayType == DisplayTypes.Vertical)
            {
                EditorGUILayout.PropertyField(serVerticalDirection);
            }

            serCustomSpacing.boolValue = EditorGUILayout.Toggle("Custom Spacing", serCustomSpacing.boolValue);
            if (thisScript.customSpacing)
            {
                serMaxSpacing.floatValue = EditorGUILayout.FloatField("Max Spacing", serMaxSpacing.floatValue);
            }

            serCustomScaling.boolValue = EditorGUILayout.Toggle("Custom Scaling", serCustomScaling.boolValue);
            if (thisScript.customScaling)
            {
                serMaxScale.floatValue = EditorGUILayout.FloatField("Scale Value", serMaxScale.floatValue);
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Toggle Settings");
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            serAllowZoom.boolValue = EditorGUILayout.Toggle("Hover Zoom", serAllowZoom.boolValue);
            serCardLimitToggle.boolValue = EditorGUILayout.Toggle("Limit Card Count", serCardLimitToggle.boolValue);
            if (thisScript.cardLimitToggle)
            {
                serCardLimit.intValue = EditorGUILayout.IntField("Card Limit", serCardLimit.intValue);
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Flip Settings");
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


            EditorGUILayout.PropertyField(serFlipRules);
            if (thisScript.flipRules != FlipRules.NoFlipping)
            {
                serAllowClickFlip.boolValue = EditorGUILayout.Toggle("Flip on Click", serAllowClickFlip.boolValue);
                serOnlyFlipLast.boolValue = EditorGUILayout.Toggle("Only Flip Last", serOnlyFlipLast.boolValue);
                serAutoFlipDrawn.boolValue = EditorGUILayout.Toggle("Auto Flip Drawn", serAutoFlipDrawn.boolValue);
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Drag Rules");
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.PropertyField(serDragRules);

            if (thisScript.dragRules == DragRules.FaceUp || thisScript.dragRules == DragRules.FaceDown || thisScript.dragRules == DragRules.BothSides)
            {
                serOnlyDragLastCard.boolValue = EditorGUILayout.Toggle("Only Drag Last Card", serOnlyDragLastCard.boolValue);
                serEnableStackDrag.boolValue = EditorGUILayout.Toggle("Enable Stack Drag", serEnableStackDrag.boolValue);

                if (thisScript.enableStackDrag)
                {
                    serMaintainOffset.boolValue = EditorGUILayout.Toggle("Maintain Card Offset", serMaintainOffset.boolValue);
                }
                else
                    thisScript.maintainOffset = false;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
    #endregion
}