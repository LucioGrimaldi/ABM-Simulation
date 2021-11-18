using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "SimObjectRenders", menuName = "Sim ObjectRenders/Sim Object Render")]
public class SimObjectRender : ScriptableObject
{
    [Serializable]
    public class StringGameObjectDictionary : SerializableDictionary<string, GameObject> {};
    [Serializable]
    public class StringTextureDictionary : SerializableDictionary<string, Texture> {};
    [Serializable]
    public class StringMaterialDictionary : SerializableDictionary<string, Material> {};
    [Serializable]
    public class StringSpriteDictionary : SerializableDictionary<string, Sprite> {};
    [Serializable]
    public class StringParticleSystemDictionary : SerializableDictionary<string, ParticleSystem> {};

    [SerializeField] public Material ghostMaterial;
    [SerializeField] public Shader outlineShader;
    [SerializeField] private RenderTypeEnum renderType;
    
    [SerializeField] private StringGameObjectDictionary meshes;
    [SerializeField] private StringTextureDictionary textures;
    [SerializeField] private StringMaterialDictionary materials;
    [SerializeField] private StringSpriteDictionary sprites;
    [SerializeField] private StringParticleSystemDictionary particleSystems;

    public enum RenderTypeEnum
    {
        MESH,
        TEXTURE,
        PARTICLE_SYSTEM,
        OTHER,
        NONE
    }

    public RenderTypeEnum RenderType { get => renderType; set => renderType = value; }
    public StringGameObjectDictionary Meshes { get => meshes; set => meshes = value; }
    public StringTextureDictionary Textures { get => textures; set => textures = value; }
    public StringMaterialDictionary Materials { get => materials; set => materials = value; }
    public StringSpriteDictionary Sprites { get => sprites; set => sprites = value; }
    public StringParticleSystemDictionary Particle_systems { get => particleSystems; set => particleSystems = value; }

}
