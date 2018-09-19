using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

namespace Gamekit2D
{
    public class Demo : MonoBehaviour
    {
        [SerializeField]
        public GunBase gun;
        //private void OnCollisionEnter2D(Collision2D collision)
        //{
        //    if (collision.gameObject.GetComponent<PlayerCharacter>() != null)
        //    {
        //        collision.gameObject.GetComponent<PlayerCharacter>().InventoryController.Gun = gun;
        //        Debug.Log("yeal");
        //    }
        //}

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.GetComponent<PlayerCharacter>() != null)
            {
                collision.gameObject.GetComponent<PlayerCharacter>().InventoryController.AddGun(gun);
                Debug.Log("yeal");
            }
        }
    }
}
