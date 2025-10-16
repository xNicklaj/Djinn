using System;
using System.Collections.Generic;
using dev.nicklaj.clibs.deblog;
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
        Deblog.Log("Updating Gallery...", "Gameplay");
        var sprites = new List<Sprite>(_imageSprites);
        sprites.Reverse();
        
        for (var i = 0; i < _imageSlots.Length; i++)
        {
            _imageSlots[i].SetImage(i < _imageSprites.Count ? sprites[i] : null);
        }
    }

    public void EnqueueImage(Sprite sprite)
    {
        Deblog.Log("Enqueueing new image to Gallery.", "Gameplay");
        _imageSprites.Add(sprite);
        if (_imageSprites.Count >= _imageSlots.Length)
        {
            _imageSprites.Remove(_imageSprites[0]);
            Deblog.Log("Picture limit reached. Removing an old image from the Gallery.", "Gameplay");
        }
            
    }
}
