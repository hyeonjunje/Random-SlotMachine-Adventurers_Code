using System;
using System.Collections.Generic;
using UnityEngine;

public enum EColorKey
{
    White,
    Gray,
    Black,
    Red,
    Green,
    Blue,
    Yellow,
    SkyBlue,
    Orange,

    BuffSkill = 101,
    DeBuffSkill = 102,
    ActiveSkill = 103,

    키워드_주어 = 201,
    키워드_부사 = 202,
    키워드_동사 = 203,
    키워드_저주 = 204,
    키워드_특수저주 = 205,
    키워드_특수 = 206,

    Token_Normal = 301,
    Token_Clickable = 302,
    Token_Enemy = 303,
}

[CreateAssetMenu(fileName = "SO_ColorPaletteData", menuName = "Scriptable Objects/SO_ColorPaletteData")]
public class SO_ColorPaletteData : ScriptableObject, IInitializable
{
    [Serializable]
    public class ColorEntry
    {
        public EColorKey key;
        public string stringKey;
        public Color color;
    }

    public List<ColorEntry> colors = new List<ColorEntry>();
    private Dictionary<EColorKey, Color> _colorDict;
    private Dictionary<string, Color> _keyNameDict;

    public void Initialize()
    {
        _colorDict.Clear();
        _keyNameDict.Clear();
    }

    public Color GetColor(EColorKey key)
    {
        if (_colorDict == null)
        {
            _colorDict = new Dictionary<EColorKey, Color>();
            foreach (var entry in colors)
                _colorDict[entry.key] = entry.color;
        }

        return _colorDict.TryGetValue(key, out var value) ? value : Color.magenta;
    }

    public Color GetColor(string key)
    {
        if (_keyNameDict == null)
        {
            _keyNameDict = new Dictionary<string, Color>();
            foreach (var entry in colors)
                _keyNameDict[entry.stringKey] = entry.color;
        }

        return _keyNameDict.TryGetValue(key, out var value) ? value : Color.magenta;
    }

    
}
