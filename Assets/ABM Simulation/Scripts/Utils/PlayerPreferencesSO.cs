using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerInfo", menuName = "ScriptableObjects/PlayerInfos")]
public class PlayerPreferencesSO : ScriptableObject
{
    public string nickname;
    public bool showSimSpace, showEnvironment;

}
