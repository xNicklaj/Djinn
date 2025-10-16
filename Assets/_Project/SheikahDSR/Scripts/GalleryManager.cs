using System;
using System.Collections.Generic;
using UnityEngine;

public class GalleryManager : MonoBehaviour
{
    public ImageSlot[] _imageSlots;
    public List<Sprite> _imageSprites;

    private void Awake()
    {
        _imageSprites = new List<Sprite>();
    }

    public void UpdateGallery()
    {
        var sprites = new List<Sprite>(_imageSprites);
        sprites.Reverse();
        
        for (var i = 0; i < _imageSlots.Length; i++)
        {
            _imageSlots[i].SetImage(i < _imageSprites.Count ? sprites[i] : null);
        }
    }

    public void EnqueueImage(Sprite sprite)
    {
        _imageSprites.Add(sprite);
        if(_imageSprites.Count >= _imageSlots.Length)
            _imageSprites.Remove(_imageSprites[0]);
    }
}
