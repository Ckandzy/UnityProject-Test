using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit2D
{
    public interface IPickupItem
    {
        string Name { get; set; }

        string Description { get; set; }

        Sprite Image { get; set; }

        void OnPickup();
    }

    public interface IWeapon : IPickupItem
    {
        /// <summary>
        /// 每秒激活次数
        /// </summary>
        float UsePerSecond { get; set; }

        /// <summary>
        /// 两次激活间隔时间
        /// </summary>
        float UseGap { get; set; }

        WaitForSeconds UseWait { get; set; }

        Coroutine Use { get; set; }
    }

    public interface IGun : IWeapon
    {
        GameObject Bullet { get; set; }

        void SpawnBullet();
    }
}
