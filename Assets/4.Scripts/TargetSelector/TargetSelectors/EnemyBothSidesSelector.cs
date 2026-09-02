using System.Collections.Generic;

// 내 양옆에 캐릭터를 타겟으로 반환 (예. 감전)
public class EnemyBothSidesSelector : TargetSelector
{
    public override List<CharacterView> SelectTarget(CharacterView caster)
    {
        List<CharacterView> result = new List<CharacterView>();

        for(int i = 0; i < CharacterSystem.Instance.Enemies.Count; ++i)
        {
            if (CharacterSystem.Instance.Enemies[i] == caster)
            {
                // Left
                if(i - 1 >= 0)
                {
                    EnemyView leftEnemy = CharacterSystem.Instance.Enemies[i - 1];
                    if(leftEnemy.Enemy.IsDead == false)
                    {
                        result.Add(leftEnemy);
                    }
                }

                // Right
                if (i + 1 < CharacterSystem.Instance.Enemies.Count)
                {
                    EnemyView rightEnemy = CharacterSystem.Instance.Enemies[i + 1];
                    if (rightEnemy.Enemy.IsDead == false)
                    {
                        result.Add(rightEnemy);
                    }
                }
            }
        }

        return result;
    }
}