using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageSlot : MonoBehaviour
{
    private Image _image;
    
    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    public void SetImage([CanBeNull] Sprite image)
    {
        if (image == null)
        {
            _image.color = new Color(1, 1, 1, 0);
            return;
        }
        
        _image.color = Color.white;
        _image.sprite = image;
        _image.preserveAspect = true;
    }
}
