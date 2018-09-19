using System;
using UnityEngine;
using UnityEngine.Events;

namespace Gamekit2D
{
    public class Damageable : MonoBehaviour, IDataPersister
    {
        [Serializable]
        public class HealthEvent : UnityEvent<Damageable>
        { }

        [Serializable]
        public class DamageEvent : UnityEvent<Damager, Damageable>
        { }

        [Serializable]
        public class HealEvent : UnityEvent<int, Damageable>
        { }

        public int startingHealth = 5;
        [Tooltip("当收到伤害时无敌")]
        public bool invulnerableAfterDamage = true;
        [Tooltip("无敌效果持续时间")]
        public float invulnerabilityDuration = 3f;
        public bool disableOnDeath = false;
        [Tooltip("An offset from the object position used to set from where the distance to the damager is computed")]
        public Vector2 centreOffset = new Vector2(0f, 1f);
        public HealthEvent OnHealthSet;
        public DamageEvent OnTakeDamage;
        public DamageEvent OnDie;
        public HealEvent OnGainHealth;
        [HideInInspector]
        public DataSettings dataSettings;

        protected bool m_Invulnerable;
        protected float m_InulnerabilityTimer;
        protected int m_CurrentHealth;
        protected Vector2 m_DamageDirection;
        protected bool m_ResetHealthOnSceneReload;

        public int CurrentHealth
        {
            get { return m_CurrentHealth; }
        }

        void OnEnable()
        {
            PersistentDataManager.RegisterPersister(this);
            m_CurrentHealth = startingHealth;

            OnHealthSet.Invoke(this);

            DisableInvulnerability();
        }

        void OnDisable()
        {
            PersistentDataManager.UnregisterPersister(this);
        }

        void Update()
        {
            if (m_Invulnerable)
            {
                m_InulnerabilityTimer -= Time.deltaTime;

                if (m_InulnerabilityTimer <= 0f)
                {
                    m_Invulnerable = false;
                }
            }
        }

        /// <summary>
        /// 开启无敌
        /// </summary>
        /// <param 是否忽略无敌时间="ignoreTimer"></param>
        public void EnableInvulnerability(bool ignoreTimer = false)
        {
            m_Invulnerable = true;
            //technically don't ignore timer, just set it to an insanly big number. Allow to avoid to add more test & special case.
            m_InulnerabilityTimer = ignoreTimer ? float.MaxValue : invulnerabilityDuration;
        }

        /// <summary>
        /// 关闭无敌
        /// </summary>
        public void DisableInvulnerability()
        {
            m_Invulnerable = false;
        }

        public Vector2 GetDamageDirection()
        {
            return m_DamageDirection;
        }

        /// <summary>
        /// 造成伤害
        /// </summary>
        /// <param name="damager"></param>
        /// <param name="ignoreInvincible"></param>
        public void TakeDamage(Damager damager, bool ignoreInvincible = false)
        {
            if ((m_Invulnerable && !ignoreInvincible) || m_CurrentHealth <= 0)
                return;

            //we can reach that point if the damager was one that was ignoring invincible state.
            //We still want the callback that we were hit, but not the damage to be removed from health.
            if (!m_Invulnerable)
            {
                m_CurrentHealth -= damager.damage;
                OnHealthSet.Invoke(this);
            }

            m_DamageDirection = transform.position + (Vector3)centreOffset - damager.transform.position;

            OnTakeDamage.Invoke(damager, this);

            if (m_CurrentHealth <= 0)
            {
                OnDie.Invoke(damager, this);
                m_ResetHealthOnSceneReload = true;
                EnableInvulnerability();
                if (disableOnDeath) gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 增加生命
        /// </summary>
        /// <param name="amount"></param>
        public void GainHealth(int amount)
        {
            m_CurrentHealth += amount;

            if (m_CurrentHealth > startingHealth)
                m_CurrentHealth = startingHealth;

            OnHealthSet.Invoke(this);

            OnGainHealth.Invoke(amount, this);
        }

        /// <summary>
        /// 设置生命
        /// </summary>
        /// <param name="amount"></param>
        public void SetHealth(int amount)
        {
            m_CurrentHealth = amount;

            OnHealthSet.Invoke(this);
        }

        public DataSettings GetDataSettings()
        {
            return dataSettings;
        }

        public void SetDataSettings(string dataTag, DataSettings.PersistenceType persistenceType)
        {
            dataSettings.dataTag = dataTag;
            dataSettings.persistenceType = persistenceType;
        }

        public Data SaveData()
        {
            return new Data<int, bool>(CurrentHealth, m_ResetHealthOnSceneReload);
        }

        public void LoadData(Data data)
        {
            Data<int, bool> healthData = (Data<int, bool>)data;
            //如果m_ResetHealthOnSceneReload为真则重置生命为初始值，否则读取存储值
            m_CurrentHealth = healthData.value1 ? startingHealth : healthData.value0;
            OnHealthSet.Invoke(this);
        }


    }
}
