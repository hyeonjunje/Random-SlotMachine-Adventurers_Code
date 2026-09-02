using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoreSystem : SingletonScene<StoreSystem>
{
    private void OnEnable()
    {
        // 캐릭터 & 키워드 & 유물 구매
        ActionSystem.AttachPerformer<PurchaseKeywordGA> (PurchaseKeyword_Performer);
        ActionSystem.AttachPerformer<PurchaseArtifactGA> (PurchaseArtifact_Performer);
    }

    private void OnDisable()
    {
        // 캐릭터 & 키워드 & 유물 구매
        ActionSystem.DetachPerformer<PurchaseKeywordGA> ();
        ActionSystem.DetachPerformer<PurchaseArtifactGA> ();
    }

    private IEnumerator PurchaseArtifact_Performer(PurchaseArtifactGA purchaseArtifactGA)
    {
        if (ArtifactSystem.Instance.HasArtifact (purchaseArtifactGA.ArtifactId))
        {
            EventBus.Publish (new StSendMessageEvent ("이미 보유한 유물입니다.", EMessageType.Warning));
            yield break;
        }

        if (UIHudSystem.Instance.CanPayGold (purchaseArtifactGA.Cost))
        {
            ActionSystem.Instance.AddReaction (new ApplyGoldDeltaGA (-purchaseArtifactGA.Cost));

            ArtifactSystem.Instance.AddArtifact (purchaseArtifactGA.ArtifactId);
        }
        else
        {
            EventBus.Publish (new StSendMessageEvent ("골드가 부족합니다.", EMessageType.Warning));
        }

        yield return null;
    }

    private IEnumerator PurchaseKeyword_Performer(PurchaseKeywordGA purchaseKeywordGA)
    {
        if (UIHudSystem.Instance.CanPayGold (purchaseKeywordGA.Cost))
        {
            // 가격 지불
            ActionSystem.Instance.AddReaction (new ApplyGoldDeltaGA (-purchaseKeywordGA.Cost));

            // 키워드 추가
            ActionSystem.Instance.AddReaction(new AddSlotMachineKeywordGA(purchaseKeywordGA.NewKeyword));
        }
        else
        {
            EventBus.Publish (new StSendMessageEvent ("골드가 부족합니다.", EMessageType.Warning));
        }

        yield return null;
    }
}