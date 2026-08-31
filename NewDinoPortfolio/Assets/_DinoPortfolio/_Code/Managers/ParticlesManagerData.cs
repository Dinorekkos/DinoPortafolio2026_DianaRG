using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ParticlesManagerData", menuName = "ScriptableObjects/ParticlesManagerData", order = 1)]
public class ParticlesManagerData : ScriptableObject
{
    [SerializeField] private ParticleData[] particleData;
    public ParticleData[] ParticleData => particleData;
}

[Serializable]
public class ParticleData
{
    public string particleName;
    public GameObject particlePrefab;

    [Header("Pooling")]
    [Min(0)] public int poolSize = 10;
    public bool overrideCanExpand;
    public bool canExpand = true;

    [Header("Lifetime")]
    [Tooltip("Used only when the prefab does not have a ParticleSystem.")]
    [Min(0f)] public float fallbackLifetime = 5f;

    public int GetPoolSize()
    {
        return poolSize;
    }

    public bool CanExpand(bool defaultCanExpand)
    {
        return overrideCanExpand ? canExpand : defaultCanExpand;
    }
}
