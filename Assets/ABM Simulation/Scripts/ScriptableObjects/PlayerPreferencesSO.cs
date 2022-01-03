using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "PlayerSettings/PlayerInfos")]
public class PlayerPreferencesSO : ScriptableObject
{
    public string nickname;
    public bool showSimSpace, showEnvironment;

    public int Environment;
    public int SimSpace2D;
    public int SimSpace3D;
}
