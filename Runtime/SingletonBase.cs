using System.Runtime.CompilerServices;
using UnityEngine;

namespace FerryKit
{
    /// <summary>
    /// 모든 싱글턴의 공통 로직(생명주기, 파괴 방지, 종료 처리)을 담은 최상위 추상 클래스.
    /// 직접 상속받지 말고, SingletonDynamic<T> 혹은 SingletonStatic<T>를 상속받아야 함.
    /// GetInstance()에 대한 함수 호출 오버헤드를 제거하기 위해 인라인 최적화 강제.
    /// </summary>
    public abstract class SingletonBase<T> : MonoBehaviour where T : SingletonBase<T>
    {
        protected static T _instance;
        protected static bool _isQuitting;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static T GetInstance() => _instance is null // 유니티의 오버로드 호출을 피하기 위해 is 패턴으로 null 체크 (성능 최적화)
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
            // 앱 종료시 모든 DontDestroyOnLoad 객체의 OnDestroy가 호출되기 전에 여기서 먼저 플래그를 켜서 파괴 이후 다시 생성을 방지
            _isQuitting = true;
        }

        protected virtual void OnAwake() { }
        protected virtual void OnBeforeDestroy() { }
    }

    /// <summary>
    /// [동적 생성 버전]
    /// 인스턴스가 없으면 자동으로 새 GameObject를 생성하여 부착.
    /// 프리팹을 만들어둘 필요 없는 매니저 객체에 적합.
    /// </summary>
    public abstract class SingletonDynamic<T> : SingletonBase<T> where T : SingletonDynamic<T>
    {
        public static T Instance => GetInstance() ?? CreateInstance();

        /// <summary>
        /// Instance getter 인라인 최적화가 이루어지도록 하기 위해
        /// Cold Path 로직을 별도 함수로 분리.
        /// </summary>
        private static T CreateInstance()
        {
            if (!_isQuitting)
            {
                new GameObject(typeof(T).Name).AddComponent<T>(); // AddComponent 내에서 Awake가 불리며 _instance가 설정됨
            }
            return _instance;
        }
    }

    /// <summary>
    /// [정적 버전]
    /// 씬에 배치된 GameObject 없으면 자동으로 생성하지 않고 null 반환.
    /// 인스펙터로 데이터 연결이 필요한 매니저 객체에 적합.
    /// </summary>
    public abstract class SingletonStatic<T> : SingletonBase<T> where T : SingletonStatic<T>
    {
        public static T Instance => GetInstance() ?? Logging();

        /// <summary>
        /// Instance getter 인라인 최적화가 이루어지도록 하기 위해
        /// Cold Path 로직을 별도 함수로 분리.
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
