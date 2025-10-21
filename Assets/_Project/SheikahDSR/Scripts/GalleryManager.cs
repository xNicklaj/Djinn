using System;
using System.Collections.Generic;
using dev.nicklaj.clibs.deblog;
using UnityEngine;
using UnityEngine.Events;

public class GalleryManager : MonoBehaviour
{
    public ImageSlot[] _imageSlots;
    public GalleryData ImageSprites;

    public UnityEvent OnIsNotEmpty;

    public void UpdateGallery()
    {
        Deblog.Log("Updating Gallery...", "Gameplay");
        var sprites = new List<Sprite>(ImageSprites.Data);
        sprites.Reverse();
        
        for (var i = 0; i < _imageSlots.Length; i++)
        {
            _imageSlots[i].SetImage(i < ImageSprites.Data.Count ? sprites[i] : null);
        }
        
        if(ImageSprites.Data.Count > 0) OnIsNotEmpty.Invoke();
    }

    private void OnEnable()
    {
        UpdateGallery();
    }

    public void EnqueueImage(Sprite sprite)
    {
        Deblog.Log("Enqueueing new image to Gallery.", "Gameplay");
        ImageSprites.Data.Add(sprite);
        if (ImageSprites.Data.Count >= _imageSlots.Length)
        {
            ImageSprites.Data.Remove(ImageSprites.Data[0]);
            Deblog.Log("Picture limit reached. Removing an old image from the Gallery.", "Gameplay");
        }
            
    }
}
