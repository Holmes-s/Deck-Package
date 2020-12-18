using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.CardPackage
{
    public class CardImageFrontDictionary : MonoBehaviour
    {
        public Dictionary<int, Sprite> CardImagesDic = new Dictionary<int, Sprite>();
        public List<Sprite> images;

        void Awake()
        {
            int id = 0;

            foreach (var item in images)
            {
                CardImagesDic.Add(id, item);
                id += 1;
            }
        }

        public Sprite FindImage(int id)
        {
            return CardImagesDic[id];
        }
    }
}