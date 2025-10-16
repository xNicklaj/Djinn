using System;
using dev.nicklaj.clibs.deblog;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageSlot : MonoBehaviour
{
    public Image FullScreenPicture_Ref;
    public TriggerOnTriggerEnter Button;
    
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
            _image.sprite = null;
            return;
        }
        
        _image.color = Color.white;
        _image.sprite = image;
        _image.preserveAspect = true;
    }

    public void Touched()
    {
        if (!_image.sprite) return;
        
        Deblog.Log($"Image slot {name} touched.", "Gameplay");
        FullScreenPicture_Ref.sprite = _image.sprite;
    }
}
