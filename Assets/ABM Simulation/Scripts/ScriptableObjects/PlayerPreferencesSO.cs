using UnityEngine;

[CreateAssetMenu(fileName = "PlayerInfo", menuName = "ScriptableObjects/PlayerInfos")]
public class PlayerPreferencesSO : ScriptableObject
{
    public string nickname;
    public bool showSimSpace, showEnvironment;
    public float musicVolume, effectsVolume;

    public int Environment;
    public int SimSpace2D;
    public int SimSpace3D;
}
