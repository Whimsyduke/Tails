using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveInTrace
    : MonoBehaviour
{
    [Header("�켣������")]
    public GameObject TraceOwner;
    [Header("�ƶ�����")]
    public GameObject Mover;
    [Header("�ƶ��ٶ�")]
    public float Speed = 10;
    [Header("�Ƿ�ֹͣ")]
    public bool IsStop = true;
    [Header("�������")]
    public bool WithMouse = false;

    const float TargetScale = 2;

    /// <summary>
    /// �켣
    /// </summary>
    public Queue<Transform> Trace { private set; get; } = new Queue<Transform>();
    /// <summary>
    /// ��һ�ƶ�Ŀ��
    /// </summary>
    public Transform Next { private set; get; }

    public bool? HasInit { private set; get; } = null;

    /// <summary>
    /// ��ȡ��һ��Ŀ���Transform
    /// </summary>
    /// <returns>Transform</returns>
    private Transform NextTransform()
    {
        Transform next = Trace.Dequeue();
        Trace.Enqueue(next);
        return Trace.Peek();
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (TraceOwner == null || Mover == null || TraceOwner.transform.childCount < 2 || Speed == 0)
        {
            Debug.LogError("��ʼ����Ϊ���㣬��Ҫ����TraceOwner��Mover��Speed������TraceOwner��Ҫ�������ӽڵ㣬SpeedҪ����0");
            IsStop = false;
            HasInit = false;
            return;
        }
        LineRenderer lineRenderer = TraceOwner.GetComponent<LineRenderer>();
        int count = TraceOwner.transform.childCount;
        lineRenderer.positionCount = count + 1;
        for (int i = 0; i < count; i++)
        {
            Transform point = TraceOwner.transform.GetChild(i);
            Trace.Enqueue(point);
            lineRenderer.SetPosition(i, point.position);
        }
        lineRenderer.SetPosition(count, TraceOwner.transform.GetChild(0).position);
        Transform start = Trace.Peek();
        Mover.transform.position = start.position;
        Transform next = NextTransform();
        Material material = next.gameObject.GetComponent<MeshRenderer>().material;
        material.color = Color.red;
        next.localScale *= TargetScale;
        HasInit = true;
        Debug.Log("��ʼ���ɹ�");
    }

    // Update is called once per frame
    private void Update()
    {
        if (IsStop)
        {
            return;
        }
        else if (HasInit != true)
        {
            Debug.LogError("��ʼ��ʧ�ܣ��޷���������Ҫ���úò������ؿ���Ϸ");
            IsStop = true;
            return;
        }
        float speed = Speed * Time.deltaTime;
        Vector3 nextPos;
        if (WithMouse)
        {
            Vector3 mousePosScreen = Input.mousePosition;
            mousePosScreen.z = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(mousePosScreen);
            float distance = Vector3.Distance(mousePos, Mover.transform.position);
            if (distance > speed)
            {
                nextPos = Vector3.Lerp(Mover.transform.position, mousePos, speed / distance);
            }
            else
            {
                nextPos = mousePos;
            }
        }
        else
        {
            Transform next = Trace.Peek();
            float distance = Vector3.Distance(next.position, Mover.transform.position);
            if (distance > speed)
            {
                nextPos = Vector3.Lerp(Mover.transform.position, next.position, speed / distance);
            }
            else
            {
                Material material = next.gameObject.GetComponent<MeshRenderer>().material;
                material.color = Color.red;
                next.localScale /= TargetScale;
                Transform first = next;
                float length = speed - distance;
                Transform newNext = NextTransform();
                distance = Vector3.Distance(next.position, newNext.position);
                // ������·�̣�����speed�����ڵ�
                while (length > distance)
                {
                    length -= distance;
                    next = newNext;
                    newNext = NextTransform();
                    if (newNext == first)
                    {
                        Debug.LogError("ÿ֡�ٶȴ����ܹ켣���ȣ���Ҫ��������");
                        IsStop = true;
                        return;
                    }
                    distance = Vector3.Distance(next.position, newNext.position);
                }
                nextPos = Vector3.Lerp(Mover.transform.position, next.position, length / distance);
                material = newNext.gameObject.GetComponent<MeshRenderer>().material;
                material.color = Color.green;
                newNext.localScale = next.localScale * TargetScale;
            }
        }
        Mover.transform.rotation = Quaternion.FromToRotation(Vector3.forward, nextPos - Mover.transform.position);
        Mover.transform.position = nextPos;
    }
}
