using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit2D
{
    [SelectionBase]
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        /// <summary>
        /// 移动类型
        /// </summary>
        public enum MovingPlatformType
        {
            /// <summary>
            /// 双向往返
            /// </summary>
            BACK_FORTH,
            /// <summary>
            /// 循环
            /// </summary>
            LOOP,
            /// <summary>
            /// 单次
            /// </summary>
            ONCE
        }

        public PlatformCatcher platformCatcher;
        public float speed = 1.0f;
        public MovingPlatformType platformType;

        /// <summary>
        /// 
        /// </summary>
        public bool startMovingOnlyWhenVisible;
        public bool isMovingAtStart = true;

        [HideInInspector]
        public Vector3[] localNodes = new Vector3[1];

        public float[] waitTimes = new float[1];

        public Vector3[] worldNode {  get { return m_WorldNode; } }

        protected Vector3[] m_WorldNode;

        protected int m_Current = 0;
        protected int m_Next = 0;
        protected int m_Dir = 1;

        protected float m_WaitTime = -1.0f;

        protected Rigidbody2D m_Rigidbody2D;
        protected Vector2 m_Velocity;
        
        /// <summary>
        /// 标志是否正在运行 在FixUpdate内
        /// </summary>
        protected bool m_Started = false;
        protected bool m_VeryFirstStart = false;

        public Vector2 Velocity
        {
            get { return m_Velocity; }
        }

        private void Reset()
        {
            //we always have at least a node which is the local position
            //至少应该有一个相对坐标节点
            localNodes[0] = Vector3.zero;
            waitTimes[0] = 0;

            m_Rigidbody2D = GetComponent<Rigidbody2D>();
            m_Rigidbody2D.isKinematic = true;

            if (platformCatcher == null)
                platformCatcher = GetComponent<PlatformCatcher> ();
        }

        private void Start()
        {
            m_Rigidbody2D = GetComponent<Rigidbody2D>();
            m_Rigidbody2D.isKinematic = true;

            if (platformCatcher == null)
                platformCatcher = GetComponent<PlatformCatcher>();
      
            //Allow to make platform only move when they became visible
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            for(int i = 0; i < renderers.Length; ++i)
            {
                var b = renderers[i].gameObject.AddComponent<VisibleBubbleUp>();
                b.objectBecameVisible = BecameVisible;
            }

            //we make point in the path being defined in local space so game designer can move the platform & path together
            //but as the platform will move during gameplay, that would also move the node. So we convert the local nodes
            // (only used at edit time) to world position (only use at runtime)
            m_WorldNode = new Vector3[localNodes.Length];
            for (int i = 0; i < m_WorldNode.Length; ++i)
                m_WorldNode[i] = transform.TransformPoint(localNodes[i]);

            Init();
        }

        protected void Init()
        {
            m_Current = 0;
            m_Dir = 1;
            m_Next = localNodes.Length > 1 ? 1 : 0;

            m_WaitTime = waitTimes[0];

            m_VeryFirstStart = false;
            if (isMovingAtStart)
            {
                m_Started = !startMovingOnlyWhenVisible;
                m_VeryFirstStart = true;
            }
            else
                m_Started = false;
        }

        private void FixedUpdate()
        {
            //注：Time.deltaTime在FixUpdate()内是固定时间，取决于物理系统设置
            if (!m_Started)
                return;

            //no need to update we have a single node in the path
            if (m_Current == m_Next)
                return;

            if(m_WaitTime > 0)
            {
                m_WaitTime -= Time.deltaTime;
                return;
            }

            float distanceToGo = speed * Time.deltaTime;

            //当需要移动的距离大于0
            while(distanceToGo > 0)
            {

                Vector2 direction = m_WorldNode[m_Next] - transform.position;

                float dist = distanceToGo;
                //当 离下一个节点的距离 小于 将要移动的位移时，即继续执行会超出目标点时
                if(direction.sqrMagnitude < dist * dist)
                {   //we have to go farther than our current goal point, so we set the distance to the remaining distance
                    //then we change the current & next indexes
                    dist = direction.magnitude;

                    m_Current = m_Next;

                    m_WaitTime = waitTimes[m_Current];

                    if (m_Dir > 0)
                    {
                        m_Next += 1;
                        if (m_Next >= m_WorldNode.Length)
                        { //we reach the end

                            switch(platformType)
                            {
                                case MovingPlatformType.BACK_FORTH:
                                    m_Next = m_WorldNode.Length - 2;
                                    m_Dir = -1;
                                    break;
                                case MovingPlatformType.LOOP:
                                    m_Next = 0;
                                    break;
                                case MovingPlatformType.ONCE:
                                    m_Next -= 1;
                                    StopMoving();
                                    break;
                            }
                        }
                    }
                    else
                    {
                        m_Next -= 1;
                        if(m_Next < 0)
                        { //reached the beginning again

                            switch (platformType)
                            {
                                case MovingPlatformType.BACK_FORTH:
                                    m_Next = 1;
                                    m_Dir = 1;
                                    break;
                                case MovingPlatformType.LOOP:
                                    m_Next = m_WorldNode.Length - 1;
                                    break;
                                case MovingPlatformType.ONCE:
                                    m_Next += 1;
                                    StopMoving();
                                    break;
                            }
                        }
                    }
                }

                //移动的距离+方向
                m_Velocity = direction.normalized * dist;

                //transform.position +=  direction.normalized * dist;
                m_Rigidbody2D.MovePosition(m_Rigidbody2D.position + m_Velocity);
                platformCatcher.MoveCaughtObjects (m_Velocity);
                //We remove the distance we moved. That way if we didn't had enough distance to the next goal, we will do a new loop to finish
                //the remaining distance we have to cover this frame toward the new goal
                //这里利用浮点数减法的结果判断是否大于0来执行while循环，有待商榷
                //Debug.Log(distanceToGo + "-" + dist + "=" + (distanceToGo - dist));
                distanceToGo -= dist;
                // we have some wait time set, that mean we reach a point where we have to wait. So no need to continue to move the platform, early exit.
                //跳出，执行等待
                if (m_WaitTime > 0.001f)
                {
                    break;
                }
            }
        }

        public void StartMoving()
        {
            m_Started = true;
        }

        public void StopMoving()
        {
            m_Started = false;
        }

        public void ResetPlatform()
        {
            transform.position = m_WorldNode[0];
            Init();
        }

        /// <summary>
        /// 当Renderer在场景中被渲染时的回调
        /// </summary>
        /// <param name="obj"></param>
        private void BecameVisible(VisibleBubbleUp obj)
        {
            if (m_VeryFirstStart)
            {
                m_Started = true;
                m_VeryFirstStart = false;
            }
        }
    }
}