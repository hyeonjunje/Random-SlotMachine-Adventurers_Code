using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 슬롯머신의 스킬 토큰 목록을 관리하는 컨트롤러.
/// 토큰 생성, 드래그 정렬, 사용(제거) 및 레이아웃 갱신을 담당한다.
/// 적(Enemy) 토큰은 목표 인덱스(고정석)를 가지며, 플레이어 토큰은 자유롭게 이동 가능하다.
/// </summary>
public class SlotMachineSkillTokenController : MonoBehaviour
{
    [Header("Pivot")]
    [SerializeField] private RectTransform _skillTokenParent;

    [Header("Prefabs")]
    [SerializeField] private ListItem_SlotMachineToken _tokenPrefab;

    [Header("Settings")]
    [SerializeField] private float _itemWidth = 100f;
    [SerializeField] private float _spacing = 10f;
    [SerializeField] private float _paddingRight = 20f;
    [SerializeField] private float _paddingLeft = 20f;
    [SerializeField] private float _animationDuration = 0.3f;
    [SerializeField] private Ease _easeType = Ease.OutQuart;

    /// <summary>화면에 표시되는 순서대로 정렬된 토큰 목록 (읽기 전용)</summary>
    public IReadOnlyList<ListItem_SlotMachineToken> Tokens => _tokens;

    // 현재 활성 토큰 목록
    private List<ListItem_SlotMachineToken> _tokens = new List<ListItem_SlotMachineToken>();

    // 적 토큰이 도달해야 하는 목표 인덱스 (Key: 토큰, Value: 목표 인덱스)
    private Dictionary<ListItem_SlotMachineToken, int> _enemyTargetIndices = new Dictionary<ListItem_SlotMachineToken, int>();

    // 드래그 시작 시점에 적 토큰의 위치를 기록하는 스냅샷 (Key: 인덱스, Value: 적 토큰)
    private Dictionary<int, ListItem_SlotMachineToken> _dragSnapshotEnemyIndices = new Dictionary<int, ListItem_SlotMachineToken>();

    // =========================================================================
    //  초기화
    // =========================================================================

    /// <summary>
    /// 모든 토큰을 제거하고 상태를 초기화한다.
    /// </summary>
    public void Init()
    {
        foreach (ListItem_SlotMachineToken listItemToken in _tokens)
        {
            if (listItemToken != null)
                StartCoroutine(listItemToken.CoRelease());
        }

        _tokens.Clear();
        _enemyTargetIndices.Clear();
    }

    // =========================================================================
    //  토큰 생성
    // =========================================================================

    /// <summary>
    /// 토큰을 생성하여 목록에 추가한다.
    /// </summary>
    /// <param name="battleAct">행동 데이터</param>
    /// <param name="fixedIndex">
    /// -1이면 플레이어 토큰 (빈자리를 찾아 자동 배치),
    /// 0 이상이면 적 토큰 (해당 인덱스를 목표로 배치)
    /// </param>
    public void CreateToken(BattleAct battleAct, int fixedIndex = -1)
    {
        ListItem_SlotMachineToken newToken = Instantiate(_tokenPrefab, _skillTokenParent);
        newToken.SetBingoIndex((int)battleAct.Bingo);
        newToken.SetListItem(battleAct);

        newToken.OnDragStarted += HandleDragStart;
        newToken.OnDragMoved += HandleDragMove;
        newToken.OnDragEnded += HandleDragEnd;

        if (fixedIndex >= 0)
        {
            // 적 토큰: 목표 인덱스를 등록하고 리스트 맨 뒤에 추가
            _enemyTargetIndices[newToken] = fixedIndex;
            _tokens.Add(newToken);
        }
        else
        {
            // 플레이어 토큰: 목표 인덱스보다 앞에 있는 적 토큰을 뒤로 밀어주기 위해 삽입 위치를 탐색
            int insertIndex = FindPlayerInsertIndex();
            _tokens.Insert(insertIndex, newToken);
        }

        // 초기 위치를 오른쪽 끝 바깥으로 설정 (등장 애니메이션용)
        float startX = CalculateRightmostItemX() - (_itemWidth + _spacing);
        newToken.RectTrans.anchoredPosition = new Vector2(startX, 0);

        RefreshLayout(animate: true);
    }

    /// <summary>
    /// 플레이어 토큰의 삽입 위치를 찾는다.
    /// 목표 인덱스보다 앞에 위치한 적 토큰이 있으면, 그 앞에 삽입하여 적을 뒤로 밀어준다.
    /// </summary>
    private int FindPlayerInsertIndex()
    {
        for (int i = 0; i < _tokens.Count; i++)
        {
            var token = _tokens[i];
            if (_enemyTargetIndices.TryGetValue(token, out int targetIndex) && i < targetIndex)
            {
                return i;
            }
        }
        return _tokens.Count;
    }

    // =========================================================================
    //  토큰 사용 (제거)
    // =========================================================================

    /// <summary>
    /// 맨 앞(인덱스 0) 토큰을 사용하고 제거한다. (컨베이어 벨트 방식)
    /// </summary>
    public IEnumerator CoUseToken()
    {
        if (_tokens.Count == 0) yield break;

        ListItem_SlotMachineToken usedToken = _tokens[0];
        yield return StartCoroutine(usedToken.CoRelease());

        _tokens.RemoveAt(0);
        RefreshLayout(animate: true);
    }

    /// <summary>
    /// 해당 owner가 죽거나 무슨일이 생겼을 때 해당 토큰을 제거한다.
    /// </summary>
    /// <param name="owner"></param>
    public IEnumerator CoDeleteToken(CharacterView owner)
    {
        int removeIndex = -1;

        for(int i = 0; i < _tokens.Count; ++i)
        {
            if (_tokens[i].Item.CharacterView == owner)
            {
                removeIndex = i;
                break;
            }
        }

        if(removeIndex != -1)
        {
            yield return StartCoroutine(_tokens[removeIndex].CoRelease());

            _tokens.RemoveAt(removeIndex);
            RefreshLayout(animate: true);
        }
    }

    /// <summary>
    /// 특정 BattleAct에 해당하는 토큰을 사용하고 제거한다.
    /// 적 토큰의 고정 위치를 최대한 유지하면서 플레이어 토큰만 당겨온다.
    /// </summary>
    public IEnumerator CoUseToken(BattleAct battleAct)
    {
        if (_tokens.Count == 0) yield break;

        ListItem_SlotMachineToken usedToken = _tokens.Find(t => t.Item == battleAct);

        if (usedToken != null)
        {
            yield return StartCoroutine(usedToken.CoRelease());
            RemoveTokenPreservingEnemySlots(usedToken);
        }

        RefreshLayout(animate: true);
    }

    /// <summary>
    /// 특정 토큰을 제거하되, 적 토큰의 목표 위치를 최대한 유지하며 재배치한다.
    /// </summary>
    private void RemoveTokenPreservingEnemySlots(ListItem_SlotMachineToken targetToken)
    {
        // 살아남은 플레이어 토큰을 원래 순서대로 큐에 담는다
        Queue<ListItem_SlotMachineToken> playerQueue = new Queue<ListItem_SlotMachineToken>();
        foreach (var token in _tokens)
        {
            if (token.Item.IsPlayer && token != targetToken)
                playerQueue.Enqueue(token);
        }

        // 살아남은 적 토큰을 원래 순서대로 추출한다
        var remainingEnemies = _tokens
            .Where(t => t != targetToken && !t.Item.IsPlayer)
            .ToList();

        // 리스트 재조립: 각 슬롯마다 적의 목표 인덱스를 확인하여 우선 배치
        List<ListItem_SlotMachineToken> newTokens = new List<ListItem_SlotMachineToken>();
        int totalCount = _tokens.Count - 1;

        for (int i = 0; i < totalCount; i++)
        {
            // 이 슬롯에 배치해야 할 적 토큰이 있는지 확인
            ListItem_SlotMachineToken enemyToPlace = FindEnemyForSlot(remainingEnemies, i);

            if (enemyToPlace != null)
            {
                newTokens.Add(enemyToPlace);
                remainingEnemies.Remove(enemyToPlace);
            }
            else if (playerQueue.Count > 0)
            {
                newTokens.Add(playerQueue.Dequeue());
            }
            else if (remainingEnemies.Count > 0)
            {
                // 플레이어가 부족하면 남은 적 토큰을 앞으로 당겨 채운다
                newTokens.Add(remainingEnemies[0]);
                remainingEnemies.RemoveAt(0);
            }
        }

        // 혹시 남은 토큰이 있으면 뒤에 추가 (안전장치)
        while (playerQueue.Count > 0)
            newTokens.Add(playerQueue.Dequeue());
        foreach (var e in remainingEnemies)
            newTokens.Add(e);

        _tokens = newTokens;
    }

    /// <summary>
    /// 남은 적 목록에서 해당 슬롯(index)에 배치해야 할 적 토큰을 찾는다.
    /// 목표 인덱스가 현재 슬롯 이하인 적 중 가장 먼저 생성된 것을 반환한다.
    /// </summary>
    private ListItem_SlotMachineToken FindEnemyForSlot(List<ListItem_SlotMachineToken> enemies, int slotIndex)
    {
        foreach (var enemy in enemies)
        {
            if (_enemyTargetIndices.TryGetValue(enemy, out int target) && target <= slotIndex)
                return enemy;
        }
        return null;
    }

    // =========================================================================
    //  드래그 & 드롭
    // =========================================================================

    private void HandleDragStart(ListItem_SlotMachineToken draggedToken)
    {
        if (!draggedToken.Item.IsPlayer) return;

        // 드래그 시작 시점의 적 토큰 위치를 스냅샷으로 저장
        _dragSnapshotEnemyIndices.Clear();
        for (int i = 0; i < _tokens.Count; i++)
        {
            if (!_tokens[i].Item.IsPlayer)
                _dragSnapshotEnemyIndices.Add(i, _tokens[i]);
        }

        draggedToken.transform.SetAsLastSibling();
        draggedToken.RectTrans.DOKill();
    }

    private void HandleDragMove(ListItem_SlotMachineToken draggedToken, Vector2 screenPos)
    {
        if (!draggedToken.Item.IsPlayer) return;

        // 플레이어 토큰을 X좌표 기준으로 내림차순 정렬
        var playerTokens = _tokens
            .Where(t => t.Item.IsPlayer)
            .OrderByDescending(t => t.RectTrans.anchoredPosition.x)
            .ToList();

        // 적 토큰은 스냅샷 위치에 고정, 나머지를 플레이어로 채워 리스트 재조립
        List<ListItem_SlotMachineToken> newOrder = new List<ListItem_SlotMachineToken>();
        int playerIndex = 0;

        for (int i = 0; i < _tokens.Count; i++)
        {
            if (_dragSnapshotEnemyIndices.ContainsKey(i))
            {
                newOrder.Add(_dragSnapshotEnemyIndices[i]);
            }
            else if (playerIndex < playerTokens.Count)
            {
                newOrder.Add(playerTokens[playerIndex]);
                playerIndex++;
            }
        }

        _tokens = newOrder;
        RefreshLayout(animate: true, ignoreItem: draggedToken);
    }

    private void HandleDragEnd(ListItem_SlotMachineToken draggedToken)
    {
        RefreshLayout(animate: true);
    }

    // =========================================================================
    //  레이아웃
    // =========================================================================

    /// <summary>오른쪽 끝 아이템의 중심 X좌표를 계산한다.</summary>
    private float CalculateRightmostItemX()
    {
        return -_paddingRight - (_itemWidth / 2f);
    }

    /// <summary>
    /// 모든 토큰의 위치를 재계산하여 배치한다.
    /// </summary>
    /// <param name="animate">true이면 DOTween 애니메이션 적용</param>
    /// <param name="ignoreItem">이 토큰은 위치 갱신을 건너뛴다 (드래그 중인 토큰용)</param>
    private void RefreshLayout(bool animate, ListItem_SlotMachineToken ignoreItem = null)
    {
        float currentX = CalculateRightmostItemX();

        for (int i = 0; i < _tokens.Count; ++i)
        {
            var token = _tokens[i];
            Vector2 targetPos = new Vector2(currentX, 0);
            currentX -= (_itemWidth + _spacing);

            if (token == ignoreItem) continue;

            if (animate)
                token.RectTrans.DOAnchorPos(targetPos, _animationDuration).SetEase(_easeType);
            else
                token.RectTrans.anchoredPosition = targetPos;
        }

        RefreshParentWidth();
    }

    /// <summary>부모 RectTransform의 너비를 토큰 수에 맞게 갱신한다.</summary>
    private void RefreshParentWidth()
    {
        if (_skillTokenParent == null) return;

        float totalWidth = _tokens.Count * _itemWidth
            + Mathf.Max(0, _tokens.Count - 1) * _spacing
            + _paddingRight + _paddingLeft;

        _skillTokenParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalWidth);
    }
}