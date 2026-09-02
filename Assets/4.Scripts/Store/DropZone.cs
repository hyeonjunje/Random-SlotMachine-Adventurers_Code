using UnityEngine;

// 이제는 안쓰게 된 레거시 스크립트 (상의 후 제거 예정)
public class DropZone : MonoBehaviour
{
    [field: SerializeField] public int Index { get; private set; } = 0;

/*    // 이 Zone에 놓을 수 있는지 검사
    public bool CanDrop(ECharacterId id)
    {
        PlayerView playerView = CharacterSystem.Instance.GetPlayer(Index);
        return playerView == null || playerView.Player.PlayerData.CharacterId == id;
    }*/
}
