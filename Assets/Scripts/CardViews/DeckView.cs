using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Photon.Pun.CardPackage
{
    public class DeckView : MonoBehaviour, IPunObservable
    {
        public Deck DeckObject;
        public GameObject DeckZone;
        public bool DisplayCount;
        public GameObject CountTextZone;
        public int DeckID;

        private int cardCount = 0;

        public bool recycle = true;

        public bool allowZoom;
        public bool facingUp;
        public FlipRules flipRules;
        public DragRules dragRules;

        private PhotonView photonView;

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(DisplayCount);
                stream.SendNext(DeckID);
                stream.SendNext(recycle);
                stream.SendNext(allowZoom);
                stream.SendNext(facingUp);
                stream.SendNext(flipRules);
                stream.SendNext(dragRules);
            }
            else
            {
                DisplayCount = (bool)stream.ReceiveNext();
                DeckID = (int)stream.ReceiveNext();
                recycle = (bool)stream.ReceiveNext();
                allowZoom = (bool)stream.ReceiveNext();
                facingUp = (bool)stream.ReceiveNext();
                flipRules = (FlipRules)stream.ReceiveNext();
                dragRules = (DragRules)stream.ReceiveNext();
            }
        }

        private void OnEnable()
        {
            DeckID = gameObject.GetInstanceID();

            photonView = GetComponent<PhotonView>();
        }

        void Update()
        {
            if (cardCount != DeckObject.cards.Count)
            {
                cardCount = DeckObject.cards.Count;

                if (CountTextZone != null)
                {
                    CountTextZone.GetComponent<Text>().text = "" + DeckObject.cards.Count;
                }

                UpdateCardDisplay();
            }

            if (DisplayCount && CountTextZone != null)
                if (DisplayCount != CountTextZone.activeSelf)
                    CountTextZone.SetActive(DisplayCount);
        }

        public void setCardStates(PlayingCard card)
        {
            card.gameObject.GetComponent<CardView>().dragRules = dragRules;
            card.gameObject.GetComponent<CardView>().isFaceUp = facingUp;
            card.gameObject.GetComponent<CardView>().allowZoom = allowZoom;
        }

        public void UpdateCardDisplay()
        {
            int CardLayer = 0;
            int CardCount = DeckObject.cards.Count;
            Vector2 DeckZoneSize = DeckZone.GetComponent<RectTransform>().sizeDelta;
            Vector2 newXY = new Vector2(0, 0);
            Vector2 newSize = new Vector2(0, 0);

            //position cards
            for (int i = 0; i < CardCount; i++)
            {
                newSize.x = DeckZoneSize.x;
                newSize.y = DeckZoneSize.x * 1.4f;

                DeckObject.cards[i].GetComponent<RectTransform>().sizeDelta = newSize;
                DeckObject.cards[i].gameObject.GetComponent<RectTransform>().localPosition = new Vector3(newXY.x, newXY.y, CardLayer);
                DeckObject.cards[i].gameObject.GetComponent<RectTransform>().localEulerAngles = Vector3.zero;
            }

            SortChildren();
        }
        private void SortChildren()
        {
            for (int i = DeckObject.cards.Count - 1; i >= 0; i--)
            {
                DeckObject.cards[i].gameObject.transform.SetAsFirstSibling();
            }
        }
    }

    #region UNITY_EDITOR
#if UNITY_EDITOR
    [CustomEditor(typeof(DeckView))]
    [CanEditMultipleObjects]
    public class DeckViewEditorDisplay : Editor
    {
        #region Serialized
        SerializedProperty serDeckObject;
        SerializedProperty serDeckZone;
        SerializedProperty serDisplayCount;
        SerializedProperty serCountTextZone;
        SerializedProperty serRecycle;
        SerializedProperty serAllowZoom;
        SerializedProperty serFlipRules;
        SerializedProperty serFacingUp;
        SerializedProperty serDragRules;
        #endregion

        void OnEnable()
        {
            serDeckObject = serializedObject.FindProperty("DeckObject");
            serDeckZone = serializedObject.FindProperty("DeckZone");
            serDisplayCount = serializedObject.FindProperty("DisplayCount");
            serCountTextZone = serializedObject.FindProperty("CountTextZone");
            serRecycle = serializedObject.FindProperty("recycle");
            serAllowZoom = serializedObject.FindProperty("allowZoom");
            serFlipRules = serializedObject.FindProperty("flipRules");
            serFacingUp = serializedObject.FindProperty("facingUp");
            serDragRules = serializedObject.FindProperty("dragRules");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var thisScript = target as DeckView;

            EditorGUILayout.ObjectField(serDeckObject, new GUIContent("Deck Script"));
            EditorGUILayout.ObjectField(serDeckZone, new GUIContent("Deck Zone"));

            serDisplayCount.boolValue = EditorGUILayout.Toggle("Display Count", serDisplayCount.boolValue);
            if (thisScript.DisplayCount)
            {
                EditorGUILayout.ObjectField(serCountTextZone, new GUIContent("Count Text Zone"));
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Toggle Settings");
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            serRecycle.boolValue = EditorGUILayout.Toggle("Recycle Cards", serRecycle.boolValue);
            serAllowZoom.boolValue = EditorGUILayout.Toggle("Hover Zoom", serAllowZoom.boolValue);
            serFacingUp.boolValue = EditorGUILayout.Toggle("Default Face Up", serFacingUp.boolValue);
            EditorGUILayout.PropertyField(serFlipRules);

            EditorGUILayout.PropertyField(serDragRules);

            serializedObject.ApplyModifiedProperties();

            if (thisScript.DisplayCount)
            {
                thisScript.CountTextZone.SetActive(true);
            }
            else if (thisScript.CountTextZone != null)
            {
                thisScript.CountTextZone.SetActive(false);
            }
        }
    }
#endif
    #endregion
}