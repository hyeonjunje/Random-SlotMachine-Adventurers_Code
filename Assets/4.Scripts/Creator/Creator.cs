using System.Collections.Generic;
using UnityEngine;

public class CreateAssetName
{
    public const string PlayerView = "PlayerView";
    public const string EnemyView = "EnemyView";
    public const string GhostView = "GhostView";
    public const string DamageTextUI = "DamageTextUI";
    public const string ArtifactPopup = "ArtifactPopup";
    public const string KeywordCardPreview = "KeywordCard_Preview";
    public const string Coin = "Coin";
}


public class Creator : SingletonScene<Creator>
{
    [SerializeField] private List<GameObject> _creatorAssets;
    [SerializeField] private List<GameObject> _creatorEffectAssets;

    public T CreatAsset<T>(string objectName) where T : UnityEngine.Object
    {
        return CreatAsset<T>(objectName, Vector3.zero, Quaternion.identity);
    }

    public T CreatAsset<T>(string objectName, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
    {
        List<GameObject> objects = new List<GameObject>();
        objects.AddRange(_creatorAssets);
        objects.AddRange(_creatorEffectAssets);

        foreach (GameObject asset in objects)
        {
            if (asset.name == objectName)
            {
                GameObject result = Instantiate(asset, position, rotation);
                result.name = objectName;

                if (typeof(T) == typeof(GameObject))
                {
                    return result as T;
                }
                else
                {
                    return result.GetComponent<T>();
                }
            }
        }

        return null;
    }

    public void RemoveAsset(string objectName, GameObject obj)
    {
        Destroy(obj);
    }

    public GameObject GetPlayerEffect(EPlayerJob playerJob)
    {
        string effectName = "";

        switch (playerJob)
        {
            case EPlayerJob.Warrior:
            case EPlayerJob.Dwarf:
            case EPlayerJob.Rogue:
                effectName = "slash_02";
                break;
            case EPlayerJob.Archer:
                effectName = "comet_02";
                break;
            case EPlayerJob.Priest:
                effectName = "spark_06";
                break;
        }

        return CreatAsset<GameObject>(effectName);
    }

    public GameObject GetEnemyEffect(int enemyId)
    {
        string effectName = "";

        switch ((EEnemyId)enemyId)
        {
            case EEnemyId.Slime:
                effectName = "slime_green_01";
                break;
            case EEnemyId.Slime_Blue:
                effectName = "slime_blue_01";
                break;
            case EEnemyId.Flower:
                effectName = "confusion_01";
                break;
            case EEnemyId.Flower_Pink:
                effectName = "confusion_01";
                break;
            case EEnemyId.Golem:
                effectName = "break_glass_02";
                break;
            case EEnemyId.Mushroom:
                effectName = "confusion_01";
                break;
            case EEnemyId.Mushroom_Posion:
                effectName = "confusion_01";
                break;
            case EEnemyId.Wolf:
                effectName = "slash_03";
                break;
            case EEnemyId.Golem_Dark:
                effectName = "break_glass_02";
                break;
            case EEnemyId.KingSlime:
                effectName = "slime_blue_01";
                break;
        }

        return CreatAsset<GameObject>(effectName);
    }
}
