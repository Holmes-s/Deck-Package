using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Photon.Pun.CardPackage
{
    public class PlacementArea : MonoBehaviour, IPunObservable
    {
        #region Enums
        public enum ColorCondition
        {
            Match,
            Opposite,
            Set,
            Ignore
        }
        public enum ColorOptions
        {
            Black,
            Red
        }

        public enum SuitCondition
        {
            Match,
            Ignore
        }

        public enum ValueCondition
        {
            Match,
            Ascend,
            Descend,
            Ignore
        }
        #endregion

        public GameObject CardZone;
        public ColorCondition colorCondition = ColorCondition.Ignore;
        public ColorOptions colorOption = ColorOptions.Black;
        public SuitCondition suitCondition = SuitCondition.Ignore;
        public ValueCondition valueCondition = ValueCondition.Ignore;
        public bool allowStackPlacement = false;
        public bool stackIndividualRuleChecks;
        public bool stackStopOnFail;
        public bool useSpecificRange = false;
        public Vector2Int minMaxSuit = new Vector2Int(0, 4);
        public Vector2Int minMaxValue = new Vector2Int(0, 13);

        private PlayingCard previousCard;

        private bool isDeck = true;
        private Collection DeckOrHandCollection;

        public PhotonView photonView;

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(colorCondition);
                stream.SendNext(colorOption);
                stream.SendNext(suitCondition);
                stream.SendNext(valueCondition);
                stream.SendNext(allowStackPlacement);
                stream.SendNext(stackIndividualRuleChecks);
                stream.SendNext(stackStopOnFail);
                stream.SendNext(useSpecificRange);
                stream.SendNext(minMaxSuit.x);
                stream.SendNext(minMaxSuit.y);
                stream.SendNext(minMaxValue.x);
                stream.SendNext(minMaxValue.y);
            }
            else
            {
                if (!PhotonNetwork.IsMasterClient)
                {
                    colorCondition = (ColorCondition)stream.ReceiveNext();
                    colorOption = (ColorOptions)stream.ReceiveNext();
                    suitCondition = (SuitCondition)stream.ReceiveNext();
                    valueCondition = (ValueCondition)stream.ReceiveNext();
                    allowStackPlacement = (bool)stream.ReceiveNext();
                    stackIndividualRuleChecks = (bool)stream.ReceiveNext();
                    stackStopOnFail = (bool)stream.ReceiveNext();
                    useSpecificRange = (bool)stream.ReceiveNext();
                    minMaxSuit.x = (int)stream.ReceiveNext();
                    minMaxSuit.y = (int)stream.ReceiveNext();
                    minMaxValue.x = (int)stream.ReceiveNext();
                    minMaxValue.y = (int)stream.ReceiveNext();
                }
            }
        }

        private void OnEnable()
        {
            photonView = GetComponent<PhotonView>();
        }

        /// <summary>
        /// Set if placement area is connected to Deck or Hand
        /// </summary>
        void Start()
        {
            if (GetComponent<Deck>() != null)
            {
                isDeck = true;
                DeckOrHandCollection = GetComponent<Deck>();
            }
            else if (GetComponent<Hand>() != null)
            {
                isDeck = false;
                DeckOrHandCollection = GetComponent<Hand>();
            }
        }

        /// <summary>
        /// Update information of last placed card for condition checks
        /// </summary>
        void Update()
        {
            if (DeckOrHandCollection.cards.Count > 0)
            {
                previousCard = DeckOrHandCollection.cards[DeckOrHandCollection.cards.Count - 1];
            }
        }

        /// <summary>
        /// Add card to attached Deck or Hand
        /// </summary>
        public void AddCardToZone(PlayingCard card)
        {
            DeckOrHandCollection.Add(card);
            FormatPos();
            if (DeckOrHandCollection.cards.Count > 0)
                previousCard = card;
        }

        /// <summary>
        /// Add card to attached Deck or Hand if conditions are passed
        /// </summary>
        public bool AddCardToZoneWithRuleCheck(PlayingCard card)
        {
            if (ConditionCheck(card))
            {
                DeckOrHandCollection.Add(card);
                FormatPos();
                if (DeckOrHandCollection.cards.Count > 0)
                    previousCard = card;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if currect card matches set rules or previous card comparisons 
        /// </summary>
        public bool ConditionCheck(PlayingCard card)
        {
            bool passed = true;

            if (DeckOrHandCollection.cards.Count > 0)
                switch (colorCondition)
                {
                    case ColorCondition.Match:
                        if (ColorIdentify(card) != ColorIdentify(previousCard))
                            passed = false;
                        break;
                    case ColorCondition.Opposite:
                        if (ColorIdentify(card) == ColorIdentify(previousCard))
                            passed = false;
                        break;
                    case ColorCondition.Set:
                        if (ColorIdentify(card) != colorOption)
                            passed = false;
                        break;
                    case ColorCondition.Ignore:
                        break;
                    default:
                        break;
                }

            if (DeckOrHandCollection.cards.Count > 0)
                switch (suitCondition)
                {
                    case SuitCondition.Match:
                        if (card.Suit != previousCard.Suit)
                            passed = false;
                        break;
                    case SuitCondition.Ignore:
                        break;
                    default:
                        break;
                }

            if (DeckOrHandCollection.cards.Count > 0)
                switch (valueCondition)
                {
                    case ValueCondition.Match:
                        if (card.Value != previousCard.Value)
                            passed = false;
                        break;
                    case ValueCondition.Ascend:
                        if (card.Value != previousCard.Value + 1)
                            passed = false;
                        break;
                    case ValueCondition.Descend:
                        if (card.Value != previousCard.Value - 1)
                            passed = false;
                        break;
                    case ValueCondition.Ignore:
                        break;
                    default:
                        break;
                }

            if (useSpecificRange)
            {
                if (card.Suit < minMaxSuit.x || card.Suit > minMaxSuit.y)
                    passed = false;

                if (card.Value < minMaxValue.x || card.Value > minMaxValue.y)
                    passed = false;
            }

            return passed;
        }

        /// <summary>
        /// </summary>
        /// <returns>Color value of tested card suit</returns>
        private ColorOptions ColorIdentify(PlayingCard card)
        {
            if (card.Suit == 0 || card.Suit == 3)
                return ColorOptions.Black;
            else
                return ColorOptions.Red;
        }

        /// <summary>
        /// Updates card display format for attached Deck or Hand
        /// </summary>
        public void FormatPos()
        {
            if (isDeck)
            {
                for (int i = 0; i < GetComponent<Deck>().cards.Count; i++)
                {
                    DeckOrHandCollection.cards[i].transform.SetParent(CardZone.transform);
                }

                GetComponent<DeckView>().UpdateCardDisplay();
            }
            else
            {
                for (int i = 0; i < GetComponent<Hand>().cards.Count; i++)
                {
                    DeckOrHandCollection.cards[i].transform.SetParent(CardZone.transform.parent.transform);
                }

                GetComponent<HandView>().UpdateCardDisplay();
            }
        }
    }

    #region UNITY_EDITOR
#if UNITY_EDITOR
    [CustomEditor(typeof(PlacementArea))]
    [CanEditMultipleObjects]
    public class PlacementAreaEditorDisplay : Editor
    {
        #region Serialized
        SerializedProperty serCardZone;
        SerializedProperty serColorCondition;
        SerializedProperty serColorOption;
        SerializedProperty serSuitCondition;
        SerializedProperty serValueCondition;
        SerializedProperty serAllowStackPlacement;
        SerializedProperty serStackIndividualRuleChecks;
        SerializedProperty serStackStopOnFail;
        SerializedProperty serUseSpecificRange;
        SerializedProperty serMinMaxSuit;
        SerializedProperty serMinMaxValue;
        #endregion

        void OnEnable()
        {
            serCardZone = serializedObject.FindProperty("CardZone");
            serColorCondition = serializedObject.FindProperty("colorCondition");
            serColorOption = serializedObject.FindProperty("colorOption");
            serSuitCondition = serializedObject.FindProperty("suitCondition");
            serValueCondition = serializedObject.FindProperty("valueCondition");
            serAllowStackPlacement = serializedObject.FindProperty("allowStackPlacement");
            serStackIndividualRuleChecks = serializedObject.FindProperty("stackIndividualRuleChecks");
            serStackStopOnFail = serializedObject.FindProperty("stackStopOnFail");
            serUseSpecificRange = serializedObject.FindProperty("useSpecificRange");
            serMinMaxSuit = serializedObject.FindProperty("minMaxSuit");
            serMinMaxValue = serializedObject.FindProperty("minMaxValue");
        }

        override public void OnInspectorGUI()
        {
            serializedObject.Update();

            var thisScript = target as PlacementArea;

            EditorGUILayout.ObjectField(serCardZone, new GUIContent("Card Zone"));

            EditorGUILayout.PropertyField(serColorCondition);
            if (thisScript.colorCondition == PlacementArea.ColorCondition.Set)
            {
                EditorGUILayout.PropertyField(serColorOption);
            }

            EditorGUILayout.PropertyField(serSuitCondition);
            EditorGUILayout.PropertyField(serValueCondition);

            serAllowStackPlacement.boolValue = EditorGUILayout.Toggle("Allow Stack Placement", serAllowStackPlacement.boolValue);

            if (thisScript.allowStackPlacement)
            {
                serStackIndividualRuleChecks.boolValue = EditorGUILayout.Toggle("Individual Rule Checks", serStackIndividualRuleChecks.boolValue);
                serStackStopOnFail.boolValue = EditorGUILayout.Toggle("Stop on Single Fail", serStackStopOnFail.boolValue);
            }

            serUseSpecificRange.boolValue = EditorGUILayout.Toggle("Use Specific Range", serUseSpecificRange.boolValue);
            if (thisScript.useSpecificRange)
            {
                serMinMaxSuit.vector2IntValue = EditorGUILayout.Vector2IntField("Suit Range Restriction", serMinMaxSuit.vector2IntValue);
                serMinMaxValue.vector2IntValue = EditorGUILayout.Vector2IntField("Value Range Restriction", serMinMaxValue.vector2IntValue);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
    #endregion
}