using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit2D
{
    public abstract class ObjectPool<TPool, TObject, TInfo> : ObjectPool<TPool, TObject>
        where TPool : ObjectPool<TPool, TObject, TInfo>
        where TObject : PoolObject<TPool, TObject, TInfo>, new()
    {
        void Start()
        {
            for (int i = 0; i < initialPoolCount; i++)
            {
                TObject newPoolObject = CreateNewPoolObject();
                pool.Add(newPoolObject);
            }
        }

        public virtual TObject Pop(TInfo info)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].inPool)
                {
                    pool[i].inPool = false;
                    pool[i].WakeUp(info);
                    return pool[i];
                }
            }

            TObject newPoolObject = CreateNewPoolObject();
            pool.Add(newPoolObject);
            newPoolObject.inPool = false;
            newPoolObject.WakeUp(info);
            return newPoolObject;
        }
    }

    public abstract class ObjectPool<TPool, TObject> : MonoBehaviour
        where TPool : ObjectPool<TPool, TObject>
        where TObject : PoolObject<TPool, TObject>, new()
    {
        /// <summary>
        /// This is a reference to the prefab that is instantiated multiple times to create the pool.
        /// </summary>
        public GameObject prefab;
        /// <summary>
        /// The number of PoolObjects that are created in the Start method.
        /// </summary>
        public int initialPoolCount = 10;
        /// <summary>
        /// A list of the PoolObjects.
        /// </summary>
        [HideInInspector]
        public List<TObject> pool = new List<TObject>();

        void Start()
        {
            for (int i = 0; i < initialPoolCount; i++)
            {
                TObject newPoolObject = CreateNewPoolObject();
                pool.Add(newPoolObject);
            }
        }

        protected TObject CreateNewPoolObject()
        {
            TObject newPoolObject = new TObject();
            newPoolObject.instance = Instantiate(prefab);
            newPoolObject.instance.transform.SetParent(transform);
            newPoolObject.inPool = true;
            newPoolObject.SetReferences(this as TPool);
            newPoolObject.Sleep();
            return newPoolObject;
        }

        public virtual TObject Pop()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].inPool)
                {
                    pool[i].inPool = false;
                    pool[i].WakeUp();
                    return pool[i];
                }
            }

            TObject newPoolObject = CreateNewPoolObject();
            pool.Add(newPoolObject);
            newPoolObject.inPool = false;
            newPoolObject.WakeUp();
            return newPoolObject;
        }

        public virtual void Push(TObject poolObject)
        {
            poolObject.inPool = true;
            poolObject.Sleep();
        }
    }

    [Serializable]
    public abstract class PoolObject<TPool, TObject, TInfo> : PoolObject<TPool, TObject>
        where TPool : ObjectPool<TPool, TObject, TInfo>
        where TObject : PoolObject<TPool, TObject, TInfo>, new()
    {
        public virtual void WakeUp(TInfo info)
        { }
    }

    [Serializable]
    public abstract class PoolObject<TPool, TObject>
        where TPool : ObjectPool<TPool, TObject>
        where TObject : PoolObject<TPool, TObject>, new()
    {
        /// <summary>
        /// This bool determines whether or not the PoolObject is currently in the pool or is awake.
        /// </summary>
        public bool inPool;
        /// <summary>
        /// This GameObject is the instantiated prefab that this PoolObject wraps.
        /// </summary>
        public GameObject instance;
        /// <summary>
        /// This is the object pool to which this PoolObject belongs. It has the same type as the ObjectPool type of this class.
        /// </summary>
        public TPool objectPool;

        /// <summary>
        /// This is called once when the PoolObject is first created. 
        /// Its purpose is to cache references so that they do not need to be gathered whenever the PoolObject is awoken, although it can be used for any other one-time setup.
        /// </summary>
        /// <param name="pool"></param>
        public void SetReferences(TPool pool)
        {
            objectPool = pool;
            SetReferences();
        }

        protected virtual void SetReferences()
        { }

        /// <summary>
        /// This is called whenever the PoolObject is awoken and gathered from the pool. Its purpose is to do any setup required every time the PoolObject is used. 
        /// If the classes are given a third generic parameter then WakeUp can be called with a parameter of that type.
        /// </summary>
        public virtual void WakeUp()
        { }

        /// <summary>
        /// This is called whenever the PoolObject is returned to the pool. Its purpose is to perform any tidy up that is required after the PoolObject has been used.
        /// </summary>
        public virtual void Sleep()
        { }

        /// <summary>
        /// By default this simply returns the PoolObject to the pool but it can be overridden if additional functionality is required.
        /// </summary>
        public virtual void ReturnToPool()
        {
            TObject thisObject = this as TObject;
            objectPool.Push(thisObject);
        }
    }
}