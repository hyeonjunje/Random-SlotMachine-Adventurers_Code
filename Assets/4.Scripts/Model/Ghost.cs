public class Ghost : Character
{
    public Ghost(SO_CharacterData characterData) : base(characterData, EBattleSideType.Neutrality)
    {

    }

    public override string GetName()
    {
        return "Ghost";
    }
}
