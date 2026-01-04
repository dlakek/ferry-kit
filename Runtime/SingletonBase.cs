using FerryKit.Core;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FerryKit
{
    /// <summary>
    /// A top-level abstract class that contains the common logic for all singletons (lifecycle, destruction prevention, finalization).
    /// Do not inherit directly; instead, inherit from SingletonDynamic<T> or SingletonStatic<T>.
    /// Forces inlining optimization to eliminate the overhead of calling GetInstance().
    /// </summary>
    public abstract class SingletonBase<T> : MonoBehaviour where T : SingletonBase<T>
    {
        protected static T _instance;
        protected static bool _isQuitting;

        [MethodImpl(Opt.Inline)]
        protected static T GetInstance() => _instance is null // null check with is pattern to avoid Unity's overload call (performance optimization)
            ? _instance = FindAnyObjectByType<T>(FindObjectsInactive.Include)
            : _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            OnAwake();
        }

        private void OnDestroy()
        {
            if (_instance != this)
                return;

            _instance = null;
            OnBeforeDestroy();
        }

        private void OnApplicationQuit()
        {
            // When the app exits, turn on the flag here before OnDestroy is called for all objects to prevent them from being recreated after destruction.
            _isQuitting = true;
        }

        protected virtual void OnAwake() { }
        protected virtual void OnBeforeDestroy() { }
    }

    /// <summary>
    /// [Dynamic creation version]
    /// If an instance doesn't exist, a new GameObject is automatically created and attached.
    /// Suitable for manager objects that don't require a prefab.
    /// </summary>
    public abstract class SingletonDynamic<T> : SingletonBase<T> where T : SingletonDynamic<T>
    {
        public static T Instance
        {
            [MethodImpl(Opt.Inline)]
            get => GetInstance() ?? CreateInstance();
        }

        /// <summary>
        /// To enable instance getter inlining optimization,
        /// the cold path logic is separated into a separate function.
        /// </summary>
        private static T CreateInstance()
        {
            if (!_isQuitting)
            {
                new GameObject(typeof(T).Name).AddComponent<T>(); // Awake is called within AddComponent and _instance is set.
            }
            return _instance;
        }
    }

    /// <summary>
    /// [Static version]
    /// If there is no GameObject placed in the scene, it will not be automatically created and will return null.
    /// Suitable for manager objects that require data connection via the Inspector.
    /// </summary>
    public abstract class SingletonStatic<T> : SingletonBase<T> where T : SingletonStatic<T>
    {
        public static T Instance
        {
            [MethodImpl(Opt.Inline)]
            get => GetInstance() ?? Logging();
        }

        /// <summary>
        /// To enable instance getter inlining optimization,
        /// the cold path logic is separated into a separate function.
        /// </summary>
        private static T Logging()
        {
            if (!_isQuitting)
            {
                DevLog.LogError($"'{typeof(T)}' is missing in the scene! this singleton does not auto-create.");
            }
            return _instance;
        }
    }
}
