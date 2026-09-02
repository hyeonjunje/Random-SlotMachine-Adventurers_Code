using System.Collections.Generic;
using UnityEngine;

public class Skill
{
    public EKeyword SubjectKeyword { get; private set; } = EKeyword.None;
    public Keyword AdverbKeyword { get; private set; }
    public Keyword VerbKeyword { get; private set; }
    public string SkillName { get; private set; }
    public ECharacterAnimationType CharacterAnimationType { get; private set; }
    public int ManaCost => IsSlotMachineRerollSkill
        ? ArtifactRuntimeState.GetAdjustedSlotClickRerollManaCost (BaseManaCost)
        : BaseManaCost; public string CenterSkillIconName { get; private set; }
    public string LeftSkillIconName { get; private set; }
    public string RightSkillIconName { get; private set; }

    public Sprite SkillIcon { get; private set; }
    public string SkillDescription { get; private set; }
    public bool IsTargetRequired { get; private set; } = false;
    public bool IsDecreaseActCount { get; private set; } = false;
    public bool IsClickableSkill { get; private set; } = false;
    public bool IsSlotMachineRerollSkill { get; private set; } = false;
    public int BaseManaCost { get; private set; }
    public List<Effect> TotalEffect { get; private set; } = new List<Effect>();
    public List<Effect> ClickEffect { get; private set; } = new List<Effect>();
    public List<Keyword> ClickableKeywords { get; private set; } = new List<Keyword>();

    public Skill(SO_SkillData skillData, CharacterView owner)
    {
        SkillName = skillData.SkillName;
        CharacterAnimationType = skillData.CharacterAnimationType;
        BaseManaCost = skillData.ManaCost;
        SkillDescription = skillData.SkillDescription;
        IsSlotMachineRerollSkill = skillData.Effect is RerollSlotMachineKeywordEffect;

        CenterSkillIconName = skillData.SkillIconName;

        TotalEffect.Add(skillData.Effect);
    }

    public Skill(EnemyAct enemyAct, CharacterView owner)
    {
        SkillName = enemyAct.GetActName();
        CharacterAnimationType = enemyAct.CharacterAnimationType;
        SkillDescription = enemyAct.GetActExplain(owner);

        CenterSkillIconName = enemyAct.GetActIconName();

        TotalEffect.AddRange(enemyAct.Effects);
    }
    
    public Skill(Keyword verbKeyword, Keyword adverbKeyword, CharacterView owner)
    {
        VerbKeyword = verbKeyword;
        AdverbKeyword = adverbKeyword;

        if (owner is PlayerView playerView)
        {
            SubjectKeyword = playerView.Player.PlayerData.SubjectKeyword;
        }

        if(verbKeyword.KeywordData.IsClickableKeyword)
        {
            ClickableKeywords.Add(verbKeyword);
            BaseManaCost = 1;
        }
        if (adverbKeyword.KeywordData.IsClickableKeyword)
        {
            ClickableKeywords.Add(adverbKeyword);
            BaseManaCost = 1;
        }

        SkillName = LocalizationManager.Instance.Get(adverbKeyword.KeywordData.KeywordName) + "\n" + LocalizationManager.Instance.Get(verbKeyword.KeywordData.KeywordName);
        SkillDescription = LocalizationManager.Instance.Get(adverbKeyword.KeywordData.KeywordExplain) + "\n" + LocalizationManager.Instance.Get(verbKeyword.KeywordData.KeywordExplain);

        LeftSkillIconName = adverbKeyword.KeywordData.KeywordSpriteName;
        RightSkillIconName = verbKeyword.KeywordData.KeywordSpriteName;

        IsTargetRequired = adverbKeyword.KeywordData.IsTargetRequired | verbKeyword.KeywordData.IsTargetRequired;
        IsDecreaseActCount = adverbKeyword.KeywordData.IsDecreaseActCount & verbKeyword.KeywordData.IsDecreaseActCount;
        IsClickableSkill = adverbKeyword.KeywordData.IsClickableKeyword | verbKeyword.KeywordData.IsClickableKeyword;

        CharacterAnimationType = verbKeyword.KeywordData.CharacterAnimationType;

        int totalCount = 0;

        int dealDamageCount = 0;
        int addShieldCount = 0;
        int applyHealingCount = 0;

        foreach(Effect effect in verbKeyword.KeywordData.VerbEffects)
        {
            if(effect is DealDamageEffect dealDamageEffect)
            {
                dealDamageCount++;
            }
            else if(effect is AddShieldEffect addShieldEffect)
            {
                addShieldCount++;
            }
            else if(effect is ApplyHealingEffect applyHealingEffect)
            {
                applyHealingCount++;
            }
        }

        if (adverbKeyword.KeywordData.AdverbSkill.Effect != null)
        {
            if (adverbKeyword.KeywordData.AdverbSkill.Effect.TargetSelector is AdverbTargetSelector adverbTargetSelector)
            {
                adverbTargetSelector.SetTargetType(dealDamageCount > 0);
            }

            if ((adverbKeyword.KeywordData.AdverbSkill.AdverbEffectTargetType & EAdverbEffectTargetType.DealDamage) != 0)
            {
                totalCount += dealDamageCount;
            }
            if ((adverbKeyword.KeywordData.AdverbSkill.AdverbEffectTargetType & EAdverbEffectTargetType.AddShield) != 0)
            {
                totalCount += addShieldCount;
            }
            if ((adverbKeyword.KeywordData.AdverbSkill.AdverbEffectTargetType & EAdverbEffectTargetType.ApplyHealing) != 0)
            {
                totalCount += applyHealingCount;
            }
            if ((adverbKeyword.KeywordData.AdverbSkill.AdverbEffectTargetType & EAdverbEffectTargetType.Skill) != 0)
            {
                totalCount = 1;
            }

            // 시작 부사 Effect 추가
            if (adverbKeyword.KeywordData.AdverbSkill.AdverbEffectType == EAdverbAdjustTiming.Start)
            {
                TotalEffect.Add(adverbKeyword.KeywordData.AdverbSkill.Effect);
            }
        }

        // 동사 Effect 추가
        foreach (Effect effect in verbKeyword.KeywordData.VerbEffects)
        {
            TotalEffect.Add(effect);
        }

        if(adverbKeyword.KeywordData.AdverbSkill.Effect != null)
        {
            // 종료 부사 Effect 추가
            if (adverbKeyword.KeywordData.AdverbSkill.AdverbEffectType == EAdverbAdjustTiming.End)
            {
                for(int i = 0; i < totalCount; ++i)
                {
                    TotalEffect.Add(adverbKeyword.KeywordData.AdverbSkill.Effect);
                }
            }
        }

        // 클릭 효과
        foreach(Effect effect in adverbKeyword.KeywordData.ClickEffects)
        {
            ClickEffect.Add(effect);
        }
        foreach (Effect effect in verbKeyword.KeywordData.ClickEffects)
        {
            ClickEffect.Add(effect);
        }
    }

    public IEnumerable<EKeyword> GetUsedKeywords()
    {
        if (SubjectKeyword != EKeyword.None)
        {
            yield return SubjectKeyword;
        }

        if (AdverbKeyword?.KeywordData != null)
        {
            yield return AdverbKeyword.KeywordData.Keyword;
        }

        if (VerbKeyword?.KeywordData != null)
        {
            yield return VerbKeyword.KeywordData.Keyword;
        }
    }

    public bool UsesKeywordText(string keywordText)
    {
        if (string.IsNullOrWhiteSpace(keywordText))
        {
            return false;
        }

        string normalized = keywordText.Trim().Trim('"');
        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        if (SubjectKeyword != EKeyword.None && SubjectKeyword.ToString().Contains(normalized))
        {
            return true;
        }

        if (AdverbKeyword?.KeywordData != null &&
            (AdverbKeyword.KeywordData.KeywordName.Contains(normalized) ||
             AdverbKeyword.KeywordData.Keyword.ToString().Contains(normalized)))
        {
            return true;
        }

        if (VerbKeyword?.KeywordData != null &&
            (VerbKeyword.KeywordData.KeywordName.Contains(normalized) ||
             VerbKeyword.KeywordData.Keyword.ToString().Contains(normalized)))
        {
            return true;
        }

        return false;
    }
}
