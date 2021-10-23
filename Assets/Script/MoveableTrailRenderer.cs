using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 可移动拖尾渲染器
/// </summary>
public class MoveableTrailRenderer : MonoBehaviour
{
    #region 内部声明

    #region 常量

    /// <summary>
    /// 跳帧数量
    /// </summary>
    private const int Const_SkipFrame = 0;

    #endregion 常量

    #region 枚举

    #endregion 枚举

    #region 定义

    /// <summary>
	/// 
	/// </summary>
	internal sealed class Point
    {
        #region 内部声明

        #region 常量

        #endregion 常量

        #region 枚举

        #endregion 枚举

        #region 定义

        #endregion 定义

        #region 委托

        #endregion 委托

        #endregion 内部声明

        #region 属性字段

        #region 静态属性

        #endregion 静态属性

        #region 属性

        /// <summary>
        /// 坐标位置
        /// </summary>
        public Vector3 Position { set;  get; }

        /// <summary>
        /// 移动方向
        /// </summary>
        public Vector3 Forward { set; get; }

        /// <summary>
        /// 生成时间
        /// </summary>
        public float BirthTime { get; }

        /// <summary>
        /// 当前寿命
        /// </summary>
        public float TimeAlive
        {
            get { return Time.time - BirthTime; }
        }

        #endregion 属性

        #region 字段

        #endregion 字段

        #region 事件

        #endregion 事件

        #endregion 属性字段

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pos">初始位置</param>
        /// <param name="dir">移动方向</param>
        public Point(Vector3 pos, Vector3 dir)
        {
            Position = pos;
            Forward = dir.normalized;
            BirthTime = Time.time;
        }

        #endregion 构造函数

        #region 方法

        #region 通用方法

        /// <summary>
        /// 节点移动
        /// </summary>
        /// <param name="speed">速度</param>
        public void Move(float speed)
        {
            Vector3 move = speed * Forward;
            Position += speed * Forward;
        }

        #endregion 通用方法

        #region 重写方法

        #endregion 重写方法

        #region 事件方法

        #endregion 事件方法 

        #endregion 方法

    }

    #endregion 定义

    #region 委托

    #endregion 委托

    #endregion 内部声明

    #region 属性字段

    #region 静态属性

    #endregion 静态属性

    #region 属性

    /// <summary>
    /// 是否为启动状态(上一帧结算)
    /// </summary>
    public bool IsEnable { private set; get; }

    /// <summary>
    /// 尾部节点列表
    /// </summary>
    private Queue<Point> TrailPoints { get; } = new Queue<Point>();

    /// <summary>
    /// 头部位置
    /// </summary>
    private Point HeadPoint { set; get; } = null;

    /// <summary>
    /// 渲染器
    /// </summary>
    private LineRenderer Renderer { set; get; }

    /// <summary>
    /// 帧间距，用于跳帧
    /// </summary>
    private int FrameStep { set; get; } = 0;

    #endregion 属性

    #region 字段

    [Header("渲染器所在预制件")]
    public GameObject LineRendererPrefab;
    [Header("是否启用，每次启用会重新绘制")]
    public bool Enable = true;
    [Header("是否暂停，暂停状态下尾巴会脱离头部，取消暂停会瞬间追上头部（相当于头部瞬间到达当前位置）")]
    public bool Pause = false;
    [Header("初始化执行时直接生成在原地放置预制时间后的结果"),Range(0, int.MaxValue)]
    public float PrewarmTime = 0;
    [Header("最大长度"), Range(0, int.MaxValue)]
    public float MaxLength = 10;
    [Header("节点速度"), Range(0, int.MaxValue)]
    public float NodeSpeed = 0;
    [Header("节点距离间距，为0时不处理节点间距。"), Range(0, int.MaxValue)]
    public float NodeLengthInterval = 1;
    [Header("节点朝向间距，为0时不处理节点间距。"), Range(0, int.MaxValue)]
    public float NodeAngleInterval = 15;
    [Header("节点寿命，超过寿命的节点将会无条件移除。"), Range(0, int.MaxValue)]
    public float NodeLife = 0;

    #endregion 字段

    #region 事件

    #endregion 事件

    #endregion 属性字段

    #region 构造函数

    #endregion 构造函数

    #region 方法

    #region 通用方法

    /// <summary>
    /// 尾巴初始化
    /// </summary>
    private void Init()
    {
        if (LineRendererPrefab != null)
        {
            Renderer = Instantiate(LineRendererPrefab).GetComponent<LineRenderer>();
            if (Renderer == null)
            {
                Enable = false;
                IsEnable = false;
                return;
            }
            Renderer.enabled = true;
            Renderer.transform.parent = transform;
        }
        else
        {
            Enable = false;
            IsEnable = false;
            return;
        }
        HeadPoint = null;
        int count = 1;
        if (PrewarmTime > 0)
        {
            count = Mathf.CeilToInt(PrewarmTime / Time.deltaTime);
        }
        for (int i = 0; i < count; i++)
        {
            DoFrameLogic();
        }
        DoFrameVisual();
    }

    /// <summary>
    /// 单帧处理
    /// </summary>
    private void DoFrame()
    {
        // 关闭或重开
        if (IsEnable != Enable)
        {
            if (Enable)
            {
                Init();
            }
            else
            {
                Clean();
            }
            return;
        }
        // 暂停
        if (Pause == true) return;
        DoFrameLogic();
        DoFrameVisual();
    }

    /// <summary>
    /// 清理尾巴
    /// </summary>
    private void Clean()
    {
        TrailPoints.Clear();
        HeadPoint = null;
        if (Renderer != null)
        {
            Destroy(Renderer);
        }
    }

    /// <summary>
    /// 移除超期节点
    /// </summary>
    private void RemoveOutdatedPoints()
    {
        if (NodeLife <= 0) return;
        while(TrailPoints.Count > 0)
        {
            Point last = TrailPoints.Peek();
            if (last.TimeAlive > NodeLife)
            {
                TrailPoints.Dequeue();
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// 单帧处理逻辑
    /// </summary>
    private void DoFrameLogic()
    {
        if (Const_SkipFrame != 0 && FrameStep % Const_SkipFrame != 0)
        {
            FrameStep++;
        }
        Vector3 moverPos = transform.position;
        Vector3 moverForward = -transform.forward;
        // 移除超期节点
        RemoveOutdatedPoints();

        // 前一帧头部位置和朝向
        Vector3 headPos;
        Vector3 headForward;
        if (HeadPoint == null)
        {
            headPos = moverPos;
            headForward = moverForward;
        }
        else
        {
            headPos = HeadPoint.Position;
            headForward = HeadPoint.Forward;
        }

        // 移动现有节点
        float speed = NodeSpeed * Time.deltaTime;
        foreach (Point point in TrailPoints)
        {
            point.Move(speed);
        }

        // 计算此帧生成节点数量（考虑距离间隔和角度间隔）
        float distance = Vector3.Distance(headPos, moverPos);
        int countLength = Mathf.FloorToInt(distance / NodeLengthInterval);
        int countAngle = Mathf.FloorToInt(Vector3.Angle(headForward, moverForward) / NodeAngleInterval);
        int count = Mathf.Max(countLength, countAngle);
        count = count == 0 ? 1 : count; // 每帧至少创建一个节点

        // 生成新节点
        for (int i = 0; i < count; i++)
        {
            Vector3 nodePos = Vector3.Lerp(headPos, moverPos, (i + 1) / count);
            Vector3 nodeDict = Vector3.Lerp(headForward * 512, moverForward * 512, (i + 1) / count).normalized; // 因为二者都是标准向量所以*512提高技术精度
            HeadPoint = new Point(nodePos, nodeDict);
            TrailPoints.Enqueue(HeadPoint);
        }

        //最大距离截断
        if (MaxLength > 0) // 最大长度小于等于0时不做最大长度限制
        {
            float length = 0;
            count = 0;
            Point lastPoint = null;
            bool outRange = false;
            foreach (Point point in TrailPoints)
            {
                if (lastPoint != null)
                {
                    length += Vector3.Distance(point.Position, lastPoint.Position);
                    if (length > MaxLength)
                    {
                        outRange = true;
                        break;
                    }
                    count++;
                }
                lastPoint = point;
            }
            if (outRange && count == 0)
            {
                // 第一个节点就超过最大长度,需要使用移动器和第一个节点计算
                count = TrailPoints.Count - 1;
                while (count > 0)
                {
                    TrailPoints.Dequeue();
                    count--;
                }
            }
            else if (outRange && count < TrailPoints.Count)
            {
                // 整体长度大于最大长度
                count = TrailPoints.Count - count;
                while (count > 0)
                {
                    TrailPoints.Dequeue();
                    count--;
                }
            }
        }
    }

    /// <summary>
    /// 单帧处理视觉
    /// </summary>
    private void DoFrameVisual()
    {
        Vector3 [] points = TrailPoints.Select(r => r.Position).ToArray();
        Renderer.positionCount = points.Length;
        Renderer.SetPositions(points);
    }

    #endregion 通用方法

    #region 重写方法

    /// <summary>
    /// Unity Start
    /// </summary>
    private void Start()
    {
        IsEnable = Enable;
        Init();
    }

    /// <summary>
    /// Unity Update
    /// </summary>
    private void Update()
    {
        DoFrame();
    }

    #endregion 重写方法

    #region 事件方法

    #endregion 事件方法 

    #endregion 方法

}