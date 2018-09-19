using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit2D
{
    public class PlatformCatcher : MonoBehaviour
    {
        [Serializable]
        public class CaughtObject
        {
            public Rigidbody2D rigidbody;
            public Collider2D collider;
            public CharacterController2D character;
            public bool inContact;
            //该对象当前帧有没有进行操作
            public bool checkedThisFrame;

            public void Move (Vector2 movement)
            {
                if(!inContact)
                    return;

                if (character != null)
                    character.Move(movement);
                else
                    rigidbody.MovePosition(rigidbody.position + movement);
            }
        }


        public Rigidbody2D platformRigidbody;
        public ContactFilter2D contactFilter;

        /*protected*/public List<CaughtObject> m_CaughtObjects = new List<CaughtObject> (128);
        /*protected*/public ContactPoint2D[] m_ContactPoints = new ContactPoint2D[20];
        protected Collider2D m_Collider;
        /*protected*/public PlatformCatcher m_ParentCatcher;

        protected Action<Vector2> m_MoveDelegate = null;

        /// <summary>
        /// 承载对象中有接触的对象数量
        /// </summary>
        public int CaughtObjectCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < m_CaughtObjects.Count; i++)
                {
                    if (m_CaughtObjects[i].inContact)
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 计算承载的重质量
        /// </summary>
        public float CaughtObjectsMass
        {
            get
            {
                float mass = 0f;
                for (int i = 0; i < m_CaughtObjects.Count; i++)
                {
                    if (m_CaughtObjects[i].inContact)
                        mass += m_CaughtObjects[i].rigidbody.mass;
                }
                return mass;
            }
        }

        void Awake ()
        {
            if (platformRigidbody == null)
                platformRigidbody = GetComponent<Rigidbody2D>();

            if (m_Collider == null)
                m_Collider = GetComponent<Collider2D>();


            m_ParentCatcher = null;
            //层次递进设置m_ParentCacher
            Transform currentParent = transform.parent;
            while(currentParent != null)
            {
                PlatformCatcher catcher = currentParent.GetComponent<PlatformCatcher>();
                if (catcher != null)
                    m_ParentCatcher = catcher;
                currentParent = currentParent.parent;
            }

            //if we have a parent platform catcher, we make it's move "bubble down" to that catcher, so any object caught by that platform catacher will also
            //be moved by the parent catacher (e.g. a platform catacher on a pressure plate on top of a moving platform)
            //2018.7.21 Hotkang 疑问点
            if (m_ParentCatcher != null)
                m_ParentCatcher.m_MoveDelegate += MoveCaughtObjects;
        }

        void FixedUpdate ()
        {
            for (int i = 0, count = m_CaughtObjects.Count; i < count; i++)
            {
                CaughtObject caughtObject = m_CaughtObjects[i];
                caughtObject.inContact = false;
                caughtObject.checkedThisFrame = false;
            }
        
            CheckRigidbodyContacts (platformRigidbody);

            bool checkAgain;
            //2018.7.22 Hotkang 具体作用已明确
            do
            {
                for (int i = 0, count = m_CaughtObjects.Count; i < count; i++)
                {
                    CaughtObject caughtObject = m_CaughtObjects[i];

                    //如果caughtObject.inContact为真，且caughtObject在这个帧内没有被检查过，则继续执行CheckRigidbodyContacts检测
                    //简而言之，目的是：通过一层一层的遍历所有直接或间接与platformCatcher接触的对象，案例：platform上有一个石块，石块上站着人，此时将人也视为caughtObject
                    //checkedThisFrame目的是为了将新添加的对象，例如上文中的人，也在当前帧内进行如下操作
                    if (caughtObject.inContact)
                    {
                        if (!caughtObject.checkedThisFrame)
                        {
                            CheckRigidbodyContacts(caughtObject.rigidbody);
                            caughtObject.checkedThisFrame = true;
                        }
                    }                
                    //Some cases will remove all contacts (collider resize etc.) leading to losing contact with the platform
                    //so we check the distance of the object to the top of the platform.
                    //2018.7.22 Hotkang 此处逻辑已明确
                    //当碰撞体调整大小时会造成接触丢失，这里通过距离来判断是否接触（例如人物蹲下时，碰撞胶囊体逐渐减小）
                    if(!caughtObject.inContact)
                    {
                        Collider2D caughtObjectCollider = m_CaughtObjects[i].collider;

                        //check if we are aligned with the moving platform, otherwise the yDiff test under would be true even if far from the platform as long as we are on the same y level...
                        //作一条垂线，是否能同时穿过两个碰撞盒模型
                        bool verticalAlignement = (caughtObjectCollider.bounds.max.x > m_Collider.bounds.min.x) && (caughtObjectCollider.bounds.min.x < m_Collider.bounds.max.x);
                        if (verticalAlignement)
                        {
                            //两个碰撞盒模型在垂直方向上的最短距离
                            float yDiff = m_CaughtObjects[i].collider.bounds.min.y - m_Collider.bounds.max.y;

                            //如果距离为正（表示没有相交）且距离小于0.05则判断为相接触
                            //如果将一下语句注释，会导致人物在蹲下的过程中inContact被判定为false，产生滑动
                            if (yDiff > 0 && yDiff < 0.05f)
                            {
                                caughtObject.inContact = true;
                                caughtObject.checkedThisFrame = true;
                            }
                        }
                    }
                }

                checkAgain = false;

                for (int i = 0, count = m_CaughtObjects.Count; i < count; i++)
                {
                    CaughtObject caughtObject = m_CaughtObjects[i];
                    if (caughtObject.inContact && !caughtObject.checkedThisFrame)
                    {
                        checkAgain = true;
                        break;
                    }
                }
            }
            while (checkAgain);
        }

        /// <summary>
        /// 更新m_CaughtObjects列表，列表存储与rb碰撞的所有对象
        /// </summary>
        /// <param name="rb"></param>
        void CheckRigidbodyContacts (Rigidbody2D rb)
        {
            //获取与rb所接触的点的总数
            int contactCount = rb.GetContacts(contactFilter, m_ContactPoints);
            //if (this.gameObject.name == "PressurePad (1)")
            //    for (int i = 0; i < contactCount; i++)
            //    {
            //        if (m_ContactPoints[i].rigidbody == null || m_ContactPoints[i].otherRigidbody == null)
            //        {
            //            Debug.Log("strange contact");
            //        }
            //        else
            //        {
            //            Rigidbody2D contactRigidbody = m_ContactPoints[i].rigidbody == rb ? m_ContactPoints[i].otherRigidbody : m_ContactPoints[i].rigidbody;
            //            Debug.Log(contactRigidbody.gameObject + " : " + Mathf.Rad2Deg * Vector2.Dot(m_ContactPoints[i].normal, Vector2.up));
            //        }
            //    }
            for (int j = 0; j < contactCount; j++)
            {
                //获取rb接触点携带的碰撞信息
                ContactPoint2D contactPoint2D = m_ContactPoints[j];
                //通过碰撞信息获取与rb碰撞的对象的rigidbody 2D组件
                Rigidbody2D contactRigidbody = contactPoint2D.rigidbody == rb ? contactPoint2D.otherRigidbody : contactPoint2D.rigidbody;
                int listIndex = -1;

                //在m_CaughtObjects列表中搜寻是否有匹配的rigidbody 2D组件
                for (int k = 0; k < m_CaughtObjects.Count; k++)
                {
                    if (contactRigidbody == m_CaughtObjects[k].rigidbody)
                    {
                        listIndex = k;
                        break;
                    }
                }

                if (listIndex == -1)
                {
                    if (contactRigidbody != null)
                    {
                        if (contactRigidbody.bodyType != RigidbodyType2D.Static && contactRigidbody != platformRigidbody)
                        {
                            float dot = Vector2.Dot(contactPoint2D.normal, Vector2.down);
                            //dot > 0.8 即碰撞点平面法线与(0,-1)向量夹角小于36度角
                            if (dot > 0.8f)
                            {
                                //将该目标加入到m_CaughtObjects列表中
                                CaughtObject newCaughtObject = new CaughtObject
                                {
                                    rigidbody = contactRigidbody,
                                    character = contactRigidbody.GetComponent<CharacterController2D>(),
                                    collider = contactRigidbody.GetComponent<Collider2D>(),
                                    inContact = true,
                                    checkedThisFrame = false
                                };

                                m_CaughtObjects.Add(newCaughtObject);
                            }
                        }
                    }
                }
                else
                {
                    m_CaughtObjects[listIndex].inContact = true;
                }
            }
        }

        /// <summary>
        /// 移动所有携带对象
        /// </summary>
        /// <param name="velocity"></param>
        public void MoveCaughtObjects (Vector2 velocity)
        {
            if (m_MoveDelegate != null)
            {
                m_MoveDelegate.Invoke(velocity);
            }

            for (int i = 0, count = m_CaughtObjects.Count; i < count; i++)
            {
                CaughtObject caughtObject = m_CaughtObjects[i];
                //如果父节点Catcher不为空 且 父节点Catcher的m_CaughtObjects列表里存在Rigidbody2D组件与当前caughtObject的Rigidbody2D组件匹配
                //注：因为并没有对isContact作判断，所以并非碰撞体同时与父子两个碰撞体接触的情况。
                //游戏内案例：当moveplatform上有一个压力板（两者均有platformCatcher组件）时，如果角色同时与两个对象接触'过'，则移动由moveplatform执行
                //注：因为platfromCatcher判断是否接触还采用了垂直距离判断，所以要求压力板的碰撞盒只能略微高出moveplatform碰撞盒一点，否则角色将不会被带动。
                if (m_ParentCatcher != null && m_ParentCatcher.m_CaughtObjects.Find((CaughtObject A) => { return A.rigidbody == caughtObject.rigidbody; }) != null)
                {
                    continue;
                }

                //对CaughtObject执行移动操作
                m_CaughtObjects[i].Move(velocity);
            }
        }

        /// <summary>
        /// 是否携带该对象
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public bool HasCaughtObject (GameObject gameObject)
        {
            for (int i = 0; i < m_CaughtObjects.Count; i++)
            {
                if (m_CaughtObjects[i].collider.gameObject == gameObject && m_CaughtObjects[i].inContact)
                    return true;
            }

            return false;
        }
    }
}