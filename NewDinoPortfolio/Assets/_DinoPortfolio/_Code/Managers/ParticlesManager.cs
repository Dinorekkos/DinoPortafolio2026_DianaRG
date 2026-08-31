using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class ParticlesManager : MonoBehaviour
{
    private static ParticlesManager instance;

    public static ParticlesManager Instance
    {
        get
        {
            if (instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                instance = FindFirstObjectByType<ParticlesManager>();
#else
                instance = FindObjectOfType<ParticlesManager>();
#endif

                if (instance == null)
                {
                    GameObject managerObject = new GameObject(nameof(ParticlesManager));
                    instance = managerObject.AddComponent<ParticlesManager>();
                }
            }

            return instance;
        }
        private set => instance = value;
    }

    public bool IsInitialized { get; private set; }
    public UnityEvent OnFinishedInitializing { get; private set; } = new UnityEvent();

    [Header("Particles Manager Data")]
    [SerializeField] private ParticlesManagerData particlesManagerData;
    [SerializeField] private GameObject particlesContainer;

    [Header("Pooling")]
    [SerializeField] private bool prewarmOnInitialize = true;
    [SerializeField] private bool canExpandPools = true;
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Test")]
    public string particleNameTest;

    private readonly Dictionary<string, ParticlePool> poolsByName = new Dictionary<string, ParticlePool>();
    private readonly Dictionary<GameObject, ParticleInstance> instancesByGameObject = new Dictionary<GameObject, ParticleInstance>();
    private readonly Dictionary<ParticleInstance, Coroutine> returnRoutines = new Dictionary<ParticleInstance, Coroutine>();
    private Transform particlesContainerTransform;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        Initialize();
    }

    private void Initialize()
    {
        if (IsInitialized)
        {
            return;
        }

        if (particlesContainer == null)
        {
            particlesContainer = new GameObject("ParticlesContainer");
        }

        particlesContainerTransform = particlesContainer.transform;

        if (particlesContainerTransform.parent == null)
        {
            particlesContainerTransform.SetParent(transform);
        }

        BuildPools();

        IsInitialized = true;
        OnFinishedInitializing?.Invoke();
    }
    
    public GameObject SpawnParticle(string particleName, Vector3 position)
    {
        return SpawnParticle(particleName, position, Quaternion.identity, null);
    }

    public GameObject SpawnParticle(string particleName, Vector3 position, Quaternion rotation)
    {
        return SpawnParticle(particleName, position, rotation, null);
    }

    public GameObject SpawnParticle(string particleName, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (!IsInitialized)
        {
            Initialize();
        }

        ParticleData particleData = GetParticleDataByName(particleName);
        if (particleData == null || particleData.particlePrefab == null)
        {
            Debug.LogWarning($"Particle with name '{particleName}' not found or prefab is null.");
            return null;
        }

        ParticlePool pool = GetOrCreatePool(particleData);
        ParticleInstance particleInstance = GetAvailableParticle(pool);

        if (particleInstance == null)
        {
            Debug.LogWarning($"Particle pool '{particleName}' is empty and cannot expand.");
            return null;
        }

        Transform targetParent = parent != null ? parent : pool.Root;
        particleInstance.GameObject.transform.SetParent(targetParent, false);
        particleInstance.GameObject.transform.SetPositionAndRotation(position, rotation);
        particleInstance.GameObject.SetActive(true);

        pool.Active.Add(particleInstance);
        PlayParticleSystems(particleInstance);
        StartAutoReturn(particleInstance);

        return particleInstance.GameObject;
    }

    public GameObject SpawnParticle(string particleName, Transform parent)
    {
        if (parent == null)
        {
            return SpawnParticle(particleName, Vector3.zero, Quaternion.identity, null);
        }

        return SpawnParticle(particleName, parent.position, parent.rotation, parent);
    }

    public void DespawnParticle(GameObject particleInstance)
    {
        if (particleInstance == null)
        {
            Debug.LogWarning("Attempted to despawn a null particle instance.");
            return;
        }

        if (!instancesByGameObject.TryGetValue(particleInstance, out ParticleInstance pooledInstance))
        {
            Destroy(particleInstance);
            return;
        }

        ReleaseParticle(pooledInstance);
    }

    public void DespawnAll()
    {
        foreach (ParticlePool pool in poolsByName.Values)
        {
            ParticleInstance[] activeParticles = new ParticleInstance[pool.Active.Count];
            pool.Active.CopyTo(activeParticles);

            foreach (ParticleInstance particleInstance in activeParticles)
            {
                ReleaseParticle(particleInstance);
            }
        }
    }

    private void ReleaseParticle(ParticleInstance particleInstance)
    {
        if (particleInstance == null)
        {
            return;
        }

        ParticlePool pool = particleInstance.Pool;
        if (pool == null || !pool.Active.Remove(particleInstance))
        {
            return;
        }

        StopAutoReturn(particleInstance);
        StopAndClearParticleSystems(particleInstance);
        particleInstance.GameObject.transform.SetParent(pool.Root, false);
        particleInstance.GameObject.SetActive(false);
        pool.Available.Enqueue(particleInstance);
    }

    private ParticleData GetParticleDataByName(string particleName)
    {
        if (particlesManagerData == null || particlesManagerData.ParticleData == null)
        {
            return null;
        }

        foreach (ParticleData data in particlesManagerData.ParticleData)
        {
            if (data != null && data.particleName == particleName)
            {
                return data;
            }
        }

        return null;
    }

    private void BuildPools()
    {
        poolsByName.Clear();
        instancesByGameObject.Clear();

        if (particlesManagerData == null || particlesManagerData.ParticleData == null)
        {
            return;
        }

        foreach (ParticleData particleData in particlesManagerData.ParticleData)
        {
            if (particleData == null || particleData.particlePrefab == null || string.IsNullOrWhiteSpace(particleData.particleName))
            {
                continue;
            }

            if (poolsByName.ContainsKey(particleData.particleName))
            {
                Debug.LogWarning($"Duplicated particle name '{particleData.particleName}' in {nameof(ParticlesManagerData)}.");
                continue;
            }

            ParticlePool pool = CreatePool(particleData);
            poolsByName.Add(particleData.particleName, pool);

            if (prewarmOnInitialize)
            {
                PrewarmPool(pool, particleData.GetPoolSize());
            }
        }
    }

    private ParticlePool GetOrCreatePool(ParticleData particleData)
    {
        if (poolsByName.TryGetValue(particleData.particleName, out ParticlePool pool))
        {
            return pool;
        }

        pool = CreatePool(particleData);
        poolsByName.Add(particleData.particleName, pool);
        return pool;
    }

    private ParticlePool CreatePool(ParticleData particleData)
    {
        GameObject poolRoot = new GameObject($"{particleData.particleName}_Pool");
        poolRoot.transform.SetParent(particlesContainerTransform, false);

        return new ParticlePool(
            particleData,
            poolRoot.transform,
            particleData.CanExpand(canExpandPools));
    }

    private void PrewarmPool(ParticlePool pool, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            ParticleInstance particleInstance = CreateParticle(pool);
            pool.Available.Enqueue(particleInstance);
        }
    }

    private ParticleInstance GetAvailableParticle(ParticlePool pool)
    {
        if (pool.Available.Count > 0)
        {
            return pool.Available.Dequeue();
        }

        return pool.CanExpand ? CreateParticle(pool) : null;
    }

    private ParticleInstance CreateParticle(ParticlePool pool)
    {
        GameObject particleInstance = Instantiate(pool.Data.particlePrefab, pool.Root);
        particleInstance.SetActive(false);

        ParticleInstance pooledInstance = new ParticleInstance(
            particleInstance,
            pool,
            pool.Data.fallbackLifetime);

        instancesByGameObject.Add(particleInstance, pooledInstance);
        return pooledInstance;
    }

    private void PlayParticleSystems(ParticleInstance particleInstance)
    {
        if (particleInstance.ParticleSystems.Length == 0)
        {
            return;
        }

        StopAndClearParticleSystems(particleInstance);

        for (int i = 0; i < particleInstance.ParticleSystems.Length; i++)
        {
            if (particleInstance.ParticleSystems[i] != null)
            {
                particleInstance.ParticleSystems[i].Play(true);
            }
        }
    }

    private void StopAndClearParticleSystems(ParticleInstance particleInstance)
    {
        if (particleInstance.ParticleSystems.Length == 0)
        {
            return;
        }

        for (int i = 0; i < particleInstance.ParticleSystems.Length; i++)
        {
            if (particleInstance.ParticleSystems[i] != null)
            {
                particleInstance.ParticleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private void StartAutoReturn(ParticleInstance particleInstance)
    {
        StopAutoReturn(particleInstance);

        if (particleInstance.ParticleSystems.Length > 0)
        {
            returnRoutines[particleInstance] = StartCoroutine(ReturnWhenParticlesStop(particleInstance));
            return;
        }

        if (particleInstance.FallbackLifetime > 0f)
        {
            returnRoutines[particleInstance] = StartCoroutine(ReturnAfterFallbackLifetime(particleInstance));
        }
    }

    private void StopAutoReturn(ParticleInstance particleInstance)
    {
        if (!returnRoutines.TryGetValue(particleInstance, out Coroutine returnRoutine))
        {
            return;
        }

        StopCoroutine(returnRoutine);
        returnRoutines.Remove(particleInstance);
    }

    private IEnumerator ReturnWhenParticlesStop(ParticleInstance particleInstance)
    {
        yield return null;

        while (IsAnyParticleSystemAlive(particleInstance))
        {
            yield return null;
        }

        returnRoutines.Remove(particleInstance);
        ReleaseParticle(particleInstance);
    }

    private IEnumerator ReturnAfterFallbackLifetime(ParticleInstance particleInstance)
    {
        yield return new WaitForSeconds(particleInstance.FallbackLifetime);
        returnRoutines.Remove(particleInstance);
        ReleaseParticle(particleInstance);
    }

    private bool IsAnyParticleSystemAlive(ParticleInstance particleInstance)
    {
        for (int i = 0; i < particleInstance.ParticleSystems.Length; i++)
        {
            if (particleInstance.ParticleSystems[i] != null && particleInstance.ParticleSystems[i].IsAlive(true))
            {
                return true;
            }
        }

        return false;
    }

    #region Test

    [Button]
    private void TestParticleSpawn()
    {
        if (!string.IsNullOrWhiteSpace(particleNameTest))
        {
            SpawnParticle(particleNameTest, Vector3.zero);
        }
    }

    #endregion

    private sealed class ParticlePool
    {
        public readonly ParticleData Data;
        public readonly Queue<ParticleInstance> Available = new Queue<ParticleInstance>();
        public readonly HashSet<ParticleInstance> Active = new HashSet<ParticleInstance>();
        public readonly Transform Root;
        public readonly bool CanExpand;

        public ParticlePool(ParticleData data, Transform root, bool canExpand)
        {
            Data = data;
            Root = root;
            CanExpand = canExpand;
        }
    }

    private sealed class ParticleInstance
    {
        public readonly GameObject GameObject;
        public readonly ParticlePool Pool;
        public readonly ParticleSystem[] ParticleSystems;
        public readonly float FallbackLifetime;

        public ParticleInstance(GameObject gameObject, ParticlePool pool, float fallbackLifetime)
        {
            GameObject = gameObject;
            Pool = pool;
            FallbackLifetime = fallbackLifetime;
            ParticleSystems = gameObject.GetComponentsInChildren<ParticleSystem>(true);
        }
    }
}
