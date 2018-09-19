using System;
using UnityEngine;
using UnityEngine.Events;

namespace Gamekit2D
{
    public class Damager : MonoBehaviour
    {
        [Serializable]
        public class DamagableEvent : UnityEvent<Damager, Damageable>
        { }


        [Serializable]
        public class NonDamagableEvent : UnityEvent<Damager>
        { }

        //call that from inside the onDamageableHIt or OnNonDamageableHit to get what was hit.
        public Collider2D LastHit { get { return m_LastHit; } }

        public int damage = 1;
        [Tooltip("攻击范围指示框的偏移量")]
        public Vector2 offset = new Vector2(1.5f, 1f);
        [Tooltip("攻击范围指示框的大小")]
        public Vector2 size = new Vector2(2.5f, 1f);
        /// <summary>
        /// 根据Sprite翻转调整偏移方向(正负)
        /// </summary>
        [Tooltip("If this is set, the offset x will be changed base on the sprite flipX setting. e.g. Allow to make the damager alway forward in the direction of sprite")]
        public bool offsetBasedOnSpriteFacing = true;
        [Tooltip("SpriteRenderer used to read the flipX value used by offsetBasedOnSpriteFacing")]
        public SpriteRenderer spriteRenderer;
        [Tooltip("If disabled, damager ignore trigger when casting for damage 如果关闭，投掷攻击将会忽略触发器")]
        public bool canHitTriggers;
        public bool disableDamageAfterHit = false;
        //be forced to 被迫。。。 in addition to 除了。。。
        [Tooltip("If set, the player will be forced to respawn to latest checkpoint in addition to loosing life")]
        public bool forceRespawn = false;
        [Tooltip("If set, an invincible damageable hit will still get the onHit message (but won't lose any life) 如果开启，无敌状态的被攻击对象仍然能接收到onHit消息，但不会损失生命")]
        public bool ignoreInvincibility = false;
        public LayerMask hittableLayers;
        public DamagableEvent OnDamageableHit;
        public NonDamagableEvent OnNonDamageableHit;

        //Sprite初始翻转状态
        protected bool m_SpriteOriginallyFlipped;
        protected bool m_CanDamage = true;
        protected ContactFilter2D m_AttackContactFilter;
        protected Collider2D[] m_AttackOverlapResults = new Collider2D[10];
        protected Transform m_DamagerTransform;
        protected Collider2D m_LastHit;


        void Awake()
        {
            m_AttackContactFilter.layerMask = hittableLayers;
            m_AttackContactFilter.useLayerMask = true;
            m_AttackContactFilter.useTriggers = canHitTriggers;

            if (offsetBasedOnSpriteFacing && spriteRenderer != null)
                m_SpriteOriginallyFlipped = spriteRenderer.flipX;

            m_DamagerTransform = transform;
        }

        /// <summary>
        /// 开启攻击
        /// </summary>
        public void EnableDamage()
        {
            m_CanDamage = true;
        }

        /// <summary>
        /// 关闭攻击
        /// </summary>
        public void DisableDamage()
        {
            m_CanDamage = false;
        }

        void FixedUpdate()
        {
            if (!m_CanDamage)
                return;

            Vector2 scale = m_DamagerTransform.lossyScale;

            Vector2 facingOffset = Vector2.Scale(offset, scale);
            if (offsetBasedOnSpriteFacing && spriteRenderer != null && spriteRenderer.flipX != m_SpriteOriginallyFlipped)
                //2018.7.24 Hotkang 这里再乘scale可能多余，因为之前已经Vector2.Scale(offset, scale);
                facingOffset = new Vector2(-offset.x * scale.x, offset.y * scale.y);

            Vector2 scaledSize = Vector2.Scale(size, scale);

            //点A,B分别是攻击范围指示框的左下和右上点
            Vector2 pointA = (Vector2)m_DamagerTransform.position + facingOffset - scaledSize * 0.5f;
            Vector2 pointB = pointA + scaledSize;

            int hitCount = Physics2D.OverlapArea(pointA, pointB, m_AttackContactFilter, m_AttackOverlapResults);

            for (int i = 0; i < hitCount; i++)
            {
                m_LastHit = m_AttackOverlapResults[i];
                Damageable damageable = m_LastHit.GetComponent<Damageable>();

                if (damageable)
                {
                    OnDamageableHit.Invoke(this, damageable);
                    damageable.TakeDamage(this, ignoreInvincibility);
                    if (disableDamageAfterHit)
                        DisableDamage();
                }
                else
                {
                    OnNonDamageableHit.Invoke(this);
                }
            }
        }

        private void Update()
        {

        }

        private void OnDrawGizmos()
        {
            
        }
    }
}
