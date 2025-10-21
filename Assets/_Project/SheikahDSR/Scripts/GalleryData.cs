using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Gallery Data", menuName = "Project/Gallery Data")]
public class GalleryData : ScriptableObject
{
    public List<Sprite> Data;

    private void Awake()
    {
        Data = new List<Sprite>();
    }
}
