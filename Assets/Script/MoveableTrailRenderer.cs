using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ���ƶ���β��Ⱦ��
/// </summary>
public class MoveableTrailRenderer : MonoBehaviour
{
    #region �ڲ�����

    #region ����

    /// <summary>
    /// ��֡����
    /// </summary>
    private const int Const_SkipFrame = 0;

    #endregion ����

    #region ö��

    #endregion ö��

    #region ����

    /// <summary>
	/// 
	/// </summary>
	internal sealed class Point
    {
        #region �ڲ�����

        #region ����

        #endregion ����

        #region ö��

        #endregion ö��

        #region ����

        #endregion ����

        #region ί��

        #endregion ί��

        #endregion �ڲ�����

        #region �����ֶ�

        #region ��̬����

        #endregion ��̬����

        #region ����

        /// <summary>
        /// ����λ��
        /// </summary>
        public Vector3 Position { set;  get; }

        /// <summary>
        /// �ƶ�����
        /// </summary>
        public Vector3 Forward { set; get; }

        /// <summary>
        /// ����ʱ��
        /// </summary>
        public float BirthTime { get; }

        /// <summary>
        /// ��ǰ����
        /// </summary>
        public float TimeAlive
        {
            get { return Time.time - BirthTime; }
        }

        #endregion ����

        #region �ֶ�

        #endregion �ֶ�

        #region �¼�

        #endregion �¼�

        #endregion �����ֶ�

        #region ���캯��

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="pos">��ʼλ��</param>
        /// <param name="dir">�ƶ�����</param>
        public Point(Vector3 pos, Vector3 dir)
        {
            Position = pos;
            Forward = dir.normalized;
            BirthTime = Time.time;
        }

        #endregion ���캯��

        #region ����

        #region ͨ�÷���

        /// <summary>
        /// �ڵ��ƶ�
        /// </summary>
        /// <param name="speed">�ٶ�</param>
        public void Move(float speed)
        {
            Vector3 move = speed * Forward;
            Position += speed * Forward;
        }

        #endregion ͨ�÷���

        #region ��д����

        #endregion ��д����

        #region �¼�����

        #endregion �¼����� 

        #endregion ����

    }

    #endregion ����

    #region ί��

    #endregion ί��

    #endregion �ڲ�����

    #region �����ֶ�

    #region ��̬����

    #endregion ��̬����

    #region ����

    /// <summary>
    /// �Ƿ�Ϊ����״̬(��һ֡����)
    /// </summary>
    public bool IsEnable { private set; get; }

    /// <summary>
    /// β���ڵ��б�
    /// </summary>
    private Queue<Point> TrailPoints { get; } = new Queue<Point>();

    /// <summary>
    /// ͷ��λ��
    /// </summary>
    private Point HeadPoint { set; get; } = null;

    /// <summary>
    /// ��Ⱦ��
    /// </summary>
    private LineRenderer Renderer { set; get; }

    /// <summary>
    /// ֡��࣬������֡
    /// </summary>
    private int FrameStep { set; get; } = 0;

    #endregion ����

    #region �ֶ�

    [Header("��Ⱦ������Ԥ�Ƽ�")]
    public GameObject LineRendererPrefab;
    [Header("�Ƿ����ã�ÿ�����û����»���")]
    public bool Enable = true;
    [Header("�Ƿ���ͣ����ͣ״̬��β�ͻ�����ͷ����ȡ����ͣ��˲��׷��ͷ�����൱��ͷ��˲�䵽�ﵱǰλ�ã�")]
    public bool Pause = false;
    [Header("��ʼ��ִ��ʱֱ��������ԭ�ط���Ԥ��ʱ���Ľ��"),Range(0, int.MaxValue)]
    public float PrewarmTime = 0;
    [Header("��󳤶�"), Range(0, int.MaxValue)]
    public float MaxLength = 10;
    [Header("�ڵ��ٶ�"), Range(0, int.MaxValue)]
    public float NodeSpeed = 0;
    [Header("�ڵ�����࣬Ϊ0ʱ������ڵ��ࡣ"), Range(0, int.MaxValue)]
    public float NodeLengthInterval = 1;
    [Header("�ڵ㳯���࣬Ϊ0ʱ������ڵ��ࡣ"), Range(0, int.MaxValue)]
    public float NodeAngleInterval = 15;
    [Header("�ڵ����������������Ľڵ㽫���������Ƴ���"), Range(0, int.MaxValue)]
    public float NodeLife = 0;

    #endregion �ֶ�

    #region �¼�

    #endregion �¼�

    #endregion �����ֶ�

    #region ���캯��

    #endregion ���캯��

    #region ����

    #region ͨ�÷���

    /// <summary>
    /// β�ͳ�ʼ��
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
    /// ��֡����
    /// </summary>
    private void DoFrame()
    {
        // �رջ��ؿ�
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
        // ��ͣ
        if (Pause == true) return;
        DoFrameLogic();
        DoFrameVisual();
    }

    /// <summary>
    /// ����β��
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
    /// �Ƴ����ڽڵ�
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
    /// ��֡�����߼�
    /// </summary>
    private void DoFrameLogic()
    {
        if (Const_SkipFrame != 0 && FrameStep % Const_SkipFrame != 0)
        {
            FrameStep++;
        }
        Vector3 moverPos = transform.position;
        Vector3 moverForward = -transform.forward;
        // �Ƴ����ڽڵ�
        RemoveOutdatedPoints();

        // ǰһ֡ͷ��λ�úͳ���
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

        // �ƶ����нڵ�
        float speed = NodeSpeed * Time.deltaTime;
        foreach (Point point in TrailPoints)
        {
            point.Move(speed);
        }

        // �����֡���ɽڵ����������Ǿ������ͽǶȼ����
        float distance = Vector3.Distance(headPos, moverPos);
        int countLength = Mathf.FloorToInt(distance / NodeLengthInterval);
        int countAngle = Mathf.FloorToInt(Vector3.Angle(headForward, moverForward) / NodeAngleInterval);
        int count = Mathf.Max(countLength, countAngle);
        count = count == 0 ? 1 : count; // ÿ֡���ٴ���һ���ڵ�

        // �����½ڵ�
        for (int i = 0; i < count; i++)
        {
            Vector3 nodePos = Vector3.Lerp(headPos, moverPos, (i + 1) / count);
            Vector3 nodeDict = Vector3.Lerp(headForward * 512, moverForward * 512, (i + 1) / count).normalized; // ��Ϊ���߶��Ǳ�׼��������*512��߼�������
            HeadPoint = new Point(nodePos, nodeDict);
            TrailPoints.Enqueue(HeadPoint);
        }

        //������ض�
        if (MaxLength > 0) // ��󳤶�С�ڵ���0ʱ������󳤶�����
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
                // ��һ���ڵ�ͳ�����󳤶�,��Ҫʹ���ƶ����͵�һ���ڵ����
                count = TrailPoints.Count - 1;
                while (count > 0)
                {
                    TrailPoints.Dequeue();
                    count--;
                }
            }
            else if (outRange && count < TrailPoints.Count)
            {
                // ���峤�ȴ�����󳤶�
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
    /// ��֡�����Ӿ�
    /// </summary>
    private void DoFrameVisual()
    {
        Vector3 [] points = TrailPoints.Select(r => r.Position).ToArray();
        Renderer.positionCount = points.Length;
        Renderer.SetPositions(points);
    }

    #endregion ͨ�÷���

    #region ��д����

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

    #endregion ��д����

    #region �¼�����

    #endregion �¼����� 

    #endregion ����

}