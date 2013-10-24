﻿using UnityEngine;
using System.Collections;

//TO-DO: customize the GUI Style of the displayed text
public class ObjectTextDrawer : MonoBehaviour
{
    enum ObjectTextOptions { Name, Position, CustomText };

    [SerializeField]
    bool isEnabled = true;

    [SerializeField]
    ObjectTextOptions write;

    [SerializeField]
    string customText;

    [SerializeField]
    [Range(15, 50)]
    int textSize = 25;

    [SerializeField]
    bool boldText;

    [SerializeField]
    bool italicText;

    [SerializeField]
    Color textColor;

    [SerializeField]
    Color backgroundColor;


    void Awake()
    {
        if (!Utilities.PlatformIsEditor())
            Destroy(this);
    }
	
    #if UNITY_EDITOR

    /// <summary>
    /// Draw a sphere to signal the object's position on editor view
    /// </summary>
    void OnDrawGizmos()
    {
        if (!isEnabled)
            return;

        if (textColor.a < 0.05f)
            Debug.LogWarning("Text color of text in " + gameObject.name + " may not be totally visible. Check its alpha channel in the color picker.");

        if (backgroundColor.a < 0.05f)
            Debug.LogWarning("Background color of text in " + gameObject.name + " may not be totally visible. Check its alpha channel in the color picker.");

        GUIStyle gs = new GUIStyle();
        gs.normal.textColor = textColor;
        gs.fontSize = textSize;

        gs.onHover. textColor = Color.magenta;

        if (boldText && italicText)
            gs.fontStyle = FontStyle.BoldAndItalic;
        else if (boldText)
            gs.fontStyle = FontStyle.Bold;
        else if (italicText)
            gs.fontStyle = FontStyle.Italic;
        
        //Texture2D t = Utilities.CreateBackgroundTexture(backgroundColor);
        Texture2D t = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        t.SetPixel(0, 0, backgroundColor);
        t.Apply();
        t.hideFlags = HideFlags.DontSave;
        
        gs.normal.background = t;

        string text = "";
        switch (write)
        {
            case ObjectTextOptions.Name:
                text = gameObject.name;
                break;
            case ObjectTextOptions.Position:
                text = transform.position.ToString();
                break;
            case ObjectTextOptions.CustomText:
                text = customText;
                break;
            default:
                text = gameObject.name;
                break;
        }

        UnityEditor.Handles.Label(transform.position, text, gs);

        DestroyImmediate(t);
    }

    #endif
}
