using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit2D
{
    [SerializeField]
    [CreateAssetMenu(fileName = "GunBase", menuName = "Inventory/List", order = 1)]
    public class GunBase : ScriptableObject
    {
        public string Name;

        public string Description;

        public Sprite Image;

        public GameObject Bullet;

        public float UsePerSecond;

        [SerializeField]
        protected float m_UseGap;

        protected WaitForSeconds UseWait;

        [SerializeField]
        protected float m_NextUseTime;
        public void SpawnBullet()
        {
            //we check if there is a wall between the player and the bullet spawn position, if there is, we don't spawn a bullet
            //otherwise, the player can "shoot throught wall" because the arm extend to the other side of the wall
            //如果子弹产生点和角色间被阻挡则不生成子弹
            Vector2 testPosition = PlayerCharacter.PlayerInstance.transform.position;
            testPosition.y = PlayerCharacter.PlayerInstance.m_CurrentBulletSpawnPoint.position.y;
            Vector2 direction = (Vector2)PlayerCharacter.PlayerInstance.m_CurrentBulletSpawnPoint.position - testPosition;
            float distance = direction.magnitude;
            direction.Normalize();

            RaycastHit2D[] results = new RaycastHit2D[12];
            if (Physics2D.Raycast(testPosition, direction, PlayerCharacter.PlayerInstance.m_CharacterController2D.ContactFilter, results, distance) > 0)
                return;

            BulletObject bullet = PlayerCharacter.PlayerInstance.bulletPool.Pop(PlayerCharacter.PlayerInstance.m_CurrentBulletSpawnPoint.position);
            bool facingLeft = PlayerCharacter.PlayerInstance.m_CurrentBulletSpawnPoint == PlayerCharacter.PlayerInstance.facingLeftBulletSpawnPoint;
            bullet.rigidbody2D.velocity = new Vector2(facingLeft ? -PlayerCharacter.PlayerInstance.bulletSpeed : PlayerCharacter.PlayerInstance.bulletSpeed, 0f);
            bullet.spriteRenderer.flipX = facingLeft ^ bullet.bullet.spriteOriginallyFacesLeft;

            PlayerCharacter.PlayerInstance.rangedAttackAudioPlayer.PlayRandomSound();
        }

        public void Init()
        {
            m_UseGap = 1f / UsePerSecond;
            m_NextUseTime = Time.time;
            UseWait = new WaitForSeconds(m_UseGap);
        }

        public IEnumerator Shoot()
        {
            while (PlayerInput.Instance.RangedAttack.Held)
            {
                if (Time.time >= m_NextUseTime)
                {
                    SpawnBullet();
                    m_NextUseTime = Time.time + m_UseGap;
                }
                yield return null;
            }
        }

        public void OnPickup()
        {
            Init();
        }


    }
}
