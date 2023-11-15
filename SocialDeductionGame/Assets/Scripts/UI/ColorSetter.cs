using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColorSetter : MonoBehaviour
{
    public enum ColorProfile
    {
        Base,
        Primary,
        Secondary
    }
    [SerializeField] private ColorProfile _colorProfile;

    private Image _image;
    private TextMeshProUGUI _text;


    public void SetColor(WatchColors colorPallet)
    {
        if (!colorPallet)
        {
            Debug.LogWarning("SetColor Not given color pallet!");
            return;
        }

        // Grab Refrences
        if (!_image)
            _image = this.GetComponent<Image>();

        if (!_text)
            _text = this.GetComponent<TextMeshProUGUI>();

        // Change colors
        if (_image)
            SetImageColor(colorPallet);

        if (_text)
            SetTextColor(colorPallet);
    }

    private void SetImageColor(WatchColors colorPallet)
    {
        if (_colorProfile == ColorProfile.Base)
            _image.color = colorPallet.GetBaseColor();
        else if (_colorProfile == ColorProfile.Primary)
            _image.color = colorPallet.GetPrimaryColor();
        else if (_colorProfile == ColorProfile.Secondary)
            _image.color = colorPallet.GetSecondaryColor();
    }

    private void SetTextColor(WatchColors colorPallet)
    {
        if (_colorProfile == ColorProfile.Base)
            _text.color = colorPallet.GetBaseColor();
        else if (_colorProfile == ColorProfile.Primary)
            _text.color = colorPallet.GetPrimaryColor();
        else if (_colorProfile == ColorProfile.Secondary)
            _text.color = colorPallet.GetSecondaryColor();
    }
}
