using UnityEngine;
using Cinemachine;

namespace WarBroker.Battlefield
{
    /// <summary>
    /// 屏幕震动控制器 - 使用Cinemachine Impulse实现
    /// </summary>
    public class ScreenShakeController : MonoBehaviour
    {
        public static ScreenShakeController Instance { get; private set; }

        [Header("Impulse Source")]
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [Header("震动强度预设")]
        [SerializeField] private float collisionIntensity = 0.25f;
        [SerializeField] private float landingIntensity = 0.1f;
        [SerializeField] private float criticalIntensity = 0.4f;
        [SerializeField] private float lightIntensity = 0.05f;

        [Header("震动持续时间")]
        [SerializeField] private float defaultDuration = 0.2f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // 如果没有配置ImpulseSource，尝试获取或创建
            if (impulseSource == null)
            {
                impulseSource = GetComponent<CinemachineImpulseSource>();
                if (impulseSource == null)
                {
                    impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
                }
            }

            // 配置默认的Impulse设置
            ConfigureImpulseSource();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void ConfigureImpulseSource()
        {
            if (impulseSource == null) return;

            // 设置默认的冲击信号
            impulseSource.m_ImpulseDefinition.m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            impulseSource.m_ImpulseDefinition.m_ImpulseDuration = defaultDuration;
        }

        /// <summary>
        /// 通用震动方法
        /// </summary>
        /// <param name="intensity">震动强度</param>
        /// <param name="duration">持续时间（可选）</param>
        public void Shake(float intensity, float duration = -1f)
        {
            if (impulseSource == null) return;

            if (duration > 0)
            {
                impulseSource.m_ImpulseDefinition.m_ImpulseDuration = duration;
            }

            // 生成随机方向的震动
            Vector3 velocity = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0f
            ).normalized * intensity;

            impulseSource.GenerateImpulse(velocity);

            // 恢复默认持续时间
            if (duration > 0)
            {
                impulseSource.m_ImpulseDefinition.m_ImpulseDuration = defaultDuration;
            }
        }

        /// <summary>
        /// 碰撞震动 - 中等强度，用于单位碰撞
        /// </summary>
        public void ShakeOnCollision()
        {
            Shake(collisionIntensity);
        }

        /// <summary>
        /// 落地震动 - 轻微强度，用于单位落地
        /// </summary>
        public void ShakeOnLanding()
        {
            Shake(landingIntensity, 0.15f);
        }

        /// <summary>
        /// 暴击震动 - 强烈强度，用于暴击效果
        /// </summary>
        public void ShakeOnCritical()
        {
            Shake(criticalIntensity, 0.3f);
        }

        /// <summary>
        /// 轻微震动 - 用于小型冲击
        /// </summary>
        public void ShakeLight()
        {
            Shake(lightIntensity, 0.1f);
        }

        /// <summary>
        /// 定向震动 - 指定方向的震动
        /// </summary>
        /// <param name="direction">震动方向</param>
        /// <param name="intensity">震动强度</param>
        public void ShakeDirectional(Vector3 direction, float intensity)
        {
            if (impulseSource == null) return;

            impulseSource.GenerateImpulse(direction.normalized * intensity);
        }

        /// <summary>
        /// 从世界位置触发震动（强度随距离衰减）
        /// </summary>
        /// <param name="worldPosition">震动源位置</param>
        /// <param name="intensity">基础强度</param>
        public void ShakeAtPosition(Vector3 worldPosition, float intensity)
        {
            if (impulseSource == null) return;

            impulseSource.GenerateImpulseAt(worldPosition, Vector3.up * intensity);
        }
    }
}
