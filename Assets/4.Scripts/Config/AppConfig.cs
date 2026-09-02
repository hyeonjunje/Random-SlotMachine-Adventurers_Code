using UnityEngine;

public static class AppConfig
{
    public static EBootstrapperType BootStrapperType { get; private set; }

    public static SO_ConfigData_InGame InGame { get; private set; }

    public static bool IsCheatEnabled =>
        BootStrapperType != EBootstrapperType.Live &&
        InGame != null &&
        InGame.IsEnableCheat;

    public static void SetConfig(EBootstrapperType bootStrapperType, SO_ConfigData_InGame configData)
    {
        BootStrapperType = bootStrapperType;
        InGame = configData;
    }
}
