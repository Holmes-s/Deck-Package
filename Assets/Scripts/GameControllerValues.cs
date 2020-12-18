using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.CardPackage
{
    /// <summary>
    /// This is a place for the lists gameController uses since the custom editor code hates lists
    /// </summary>
    public class GameControllerValues : MonoBehaviour
    {
        public List<PlacementArea> TargetAreas;
        public List<HandView> Hands;
        public List<DeckView> Decks;
    }

    public class CardGame
    {
        public const string DECK_ID = "null";
        public const string CARD_ID = "nil";

        public const string PLAYER_READY = "IsPlayerReady";
        public const string PLAYER_LOADED_LEVEL = "PlayerLoadedLevel";
    }

    // Place for values of cards can be renamed
    #region Card Value Enums
    enum Suits
    {
        club,
        diamond,
        heart,
        spade,
        none
    }

    enum Values
    {
        ace,
        two,
        three,
        four,
        five,
        six,
        seven,
        eight,
        nine,
        ten,
        jack,
        queen,
        king,
        joker
    }
    #endregion


    #region Enums
    public enum DisplayTypes
    {
        Horizontal,
        Vertical,
        Arc
    }

    public enum AlignDirections
    {
        Left,
        Center,
        Right
    }

    public enum VerticalDirections
    {
        Up,
        Down
    }

    public enum LayeringDirection
    {
        Top,
        Bottom
    }

    //Enums for ui
    public enum DragRules
    {
        NoDragging,
        FaceUp,
        FaceDown,
        BothSides
    }

    public enum FlipRules
    {
        NoFlipping,
        FlipToUp,
        FlipToDown,
        FlipBothWays
    }
    #endregion
}