using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class SpriteManager : SingletonScene<SpriteManager>
{
    [SerializeField] private SpriteAtlas[] _spriteAtlas;

    private Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

    public Sprite GetSprite(string spriteName)
    {
        if(_sprites.ContainsKey(spriteName))
        {
            return _sprites[spriteName];
        }

        foreach(SpriteAtlas spriteAtlas in _spriteAtlas)
        {
            Sprite sprite = spriteAtlas.GetSprite(spriteName);

            if(sprite != null)
            {
                _sprites.Add(spriteName, sprite);
                break;
            }
        }

        if(_sprites.ContainsKey(spriteName))
        {
            return _sprites[spriteName];
        }
        else
        {
            Debug.Log("SpriteManager.GetSprite(), " + spriteName + " SpriteName Not Found");
            return null;
        }
    }
}
