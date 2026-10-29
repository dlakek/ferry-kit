using FerryKit.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FerryKit
{
    /// <summary>
    /// Contains shared singleton lifecycle, duplicate destruction, lookup, and application-quit handling.
    /// Do not inherit directly; instead, inherit from SingletonDynamic<T> or SingletonStatic<T>.
    /// The cached lookup stays on an inlineable hot path while creation and logging remain cold paths.
    /// </summary>
    public abstract class SingletonBase<T> : MonoBehaviour where T : SingletonBase<T>
    {
        private static T _instance;
        private static bool _isQuitting;

        protected virtual void OnAwake() { }
        protected virtual void OnBeforeDestroy() { }

        private void Awake()
        {
            SingletonResetRegistry.Register<T>();
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

        internal static void ResetStatics()
        {
            _instance = null;
            _isQuitting = false;
        }

        // Inactive objects can be destroyed before Awake/OnDestroy; reject detached Unity objects too.
        [MethodImpl(Opt.Inline)]
        private protected static T GetInstance() => _instance == null
            ? _instance = FindAnyObjectByType<T>(FindObjectsInactive.Include)
            : _instance;

        /// <summary>
        /// To enable instance getter inlining optimization, the cold path logic is separated into a separate function.
        /// </summary>
        [MethodImpl(Opt.NoInline)]
        private protected static T FallbackToCreateInstance()
        {
            if (!_isQuitting)
            {
                new GameObject(typeof(T).Name).AddComponent<T>(); // Awake is called within AddComponent and _instance is set.
            }
            return _instance;
        }

        [MethodImpl(Opt.NoInline)]
        private protected static T FallbackToLogging()
        {
            if (!_isQuitting)
            {
                DevLog.LogError($"'{typeof(T)}' is missing in the scene! this singleton does not auto-create.");
            }
            return _instance;
        }
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
            get => GetInstance() ?? FallbackToCreateInstance();
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
            get => GetInstance() ?? FallbackToLogging();
        }
    }

    /// <summary>
    /// Tracks initialized singleton types and resets their static state during subsystem registration,
    /// including when Unity enters Play Mode with domain reload disabled.
    /// </summary>
    internal static class SingletonResetRegistry
    {
        private static readonly List<Action> _resetters = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegisteredSingletons()
        {
            foreach (var resetter in _resetters)
            {
                resetter();
            }
        }

        internal static void Register<T>() where T : SingletonBase<T>
        {
            if (Registration<T>.IsRegistered)
                return;

            Registration<T>.IsRegistered = true;
            _resetters.Add(SingletonBase<T>.ResetStatics);
        }

        private static class Registration<T>
        {
            internal static bool IsRegistered { get; set; }
        }
    }
}
