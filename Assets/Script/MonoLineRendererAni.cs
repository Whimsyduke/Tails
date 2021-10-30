using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomPropertyDrawer(typeof(CustomSimulationSpaceAttribute))]
public sealed class CustomSimulationSpaceDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.serializedObject.FindProperty("simulationSpace").enumValueIndex == 2)
            EditorGUI.ObjectField(position, property, label);
    }
}
#endif

public class CustomSimulationSpaceAttribute : PropertyAttribute { }

//[ExecuteInEditMode]
public class MonoLineRendererAni : MonoBehaviour
{
    //相对之前的版本，10000次循环world由140ms缩短至100ms, local由135ms缩短至86ms
    //在custom模式下，原版本210ms，此版本210ms，维持不变
    [SerializeField, Header("节点数量"), Tooltip("彩条包含的节点总数。")]
    private int LineNodeCount = 20;
    [SerializeField, Header("尾部偏移"), Tooltip("在静止无抖动情况下，彩条尾部的位置（0,0,z）代表彩条长度为z。")]
    private Vector3 LineTailOffset;
    [SerializeField, Header("跟随速度"), Tooltip("不考虑抖动情况下，因头部移动使彩条拉长时，彩条缩回的速度（每秒缩回比例），值小于等于0意味着缩回速度无限大，彩条不可拉伸，不可形变；大于等于1意味着不会缩回仅受“最大长度”影响。")]
    private float FllowSpeed = 3;
    [SerializeField, Header("最大长度"), Tooltip("在静止无抖动情况下，彩条的最大长度，不会短于尾部偏移带来的长度变化。")]
    private Vector3 LineMaxLength = Vector3.zero;
    [SerializeField]
    private ParticleSystemSimulationSpace simulationSpace = ParticleSystemSimulationSpace.World;
    [SerializeField]
    [CustomSimulationSpace]
    private Transform customSimulationSpace;
    [SerializeField]
    [Tooltip("默认值0代表不使用固定频率更新，更新频率跟Unity的Update保持一致；填非0值，无论实际帧率多少，Tick频率都是一样的")]
    private uint _fixedTickRate = 0;

    private LineRenderer lineR;
    private Vector3[] positions;
    private Matrix4x4? matCustomSpaceWL = null;
    private Matrix4x4? matCustomSpaceOffset = null;

    private bool _need_reset = false;
    private bool bWaveOffset = false;
    private bool bPointOffsetRandom = false;
    private Matrix4x4 space;

    private float fixedTickStep { get { return _fixedTickRate == 0 ? 0f : 1f / (float)_fixedTickRate; } }
    private float _timePassed;
    //高于此值的话我们认为这个时间累计是异常的，需要舍弃
    private const float TIME_PASSED_THRESHOLD = 1f;

    void OnEnable()
    {
        _need_reset = true;
        _timePassed = 0f;
    }

    void OnDisable()
    {
        _need_reset = true;
        if (lineR != null)
        {
            lineR.positionCount = 2;
            lineR.SetPosition(0, Vector3.zero);
            lineR.SetPosition(1, Vector3.zero);
        }
    }

    private void reset()
    {
        lineR = GetComponent<LineRenderer>();
        lineR.useWorldSpace = simulationSpace != ParticleSystemSimulationSpace.Local;

        lineR.positionCount = LineNodeCount;
        positions = new Vector3[LineNodeCount];

        if (simulationSpace != ParticleSystemSimulationSpace.Local)
            space = transform.localToWorldMatrix;
        if (simulationSpace == ParticleSystemSimulationSpace.Custom)
            SetCustomSpaceOffset();

        for (int i = 0; i < LineNodeCount; i++)
        {
            if (i == 0)
            {
                if (simulationSpace == ParticleSystemSimulationSpace.Local)
                    positions[i] = Vector3.zero;
                else
                    positions[i] = new Vector3(space.m03, space.m13, space.m23);
            }
            else
            {
                if (simulationSpace == ParticleSystemSimulationSpace.Local)
                    positions[i] = positions[0] + (LineTailOffset / (LineNodeCount - 1)) * i;
                else
                    positions[i] = positions[0] + space.MultiplyVector((LineTailOffset / (LineNodeCount - 1)) * i);
            }
        }
        lineR.SetPositions(positions);
    }

    private void Update()
    {
        if (_fixedTickRate == 0)
            UpdatePoints(Time.deltaTime);
        else
            FixedStepUpdate();

    }

    /// <summary>
    /// 如果直接在Update里做，不同帧率的表现是不一样的，我们需要在逻辑上固定帧率
    /// </summary>
    private void FixedStepUpdate()
    {
        _timePassed += Time.deltaTime;
        //不应该出现大于阈值的情况，如果大于阈值，就reset一波
        if (_timePassed > TIME_PASSED_THRESHOLD)
        {
            _timePassed = 0;
            _need_reset = true;
            UpdatePoints(fixedTickStep);
        }
        else
        {
            while (_timePassed >= fixedTickStep)
            {
                _timePassed -= fixedTickStep;
                UpdatePoints(fixedTickStep);
            }
        }
    }

    private Vector3 Lerp(Vector3 a, Vector3 b, Vector3 t)
    {
        double x = a.x + (b.x - a.x) * (double)t.x;
        double y = a.y + (b.y - a.y) * (double)t.y;
        double z = a.z + (b.z - a.z) * (double)t.z;
        return new Vector3((float)x, (float)y, (float)z);
    }

    private void UpdatePoints(float deltaTime)
    {
        if (_need_reset)
        {
            reset();
            _need_reset = false;
        }

        lineR.useWorldSpace = simulationSpace != ParticleSystemSimulationSpace.Local;

        if (simulationSpace != ParticleSystemSimulationSpace.Local)
            space = transform.localToWorldMatrix;
        if (simulationSpace == ParticleSystemSimulationSpace.Custom)
            SetCustomSpaceOffset();

        if (positions.Length != LineNodeCount)
        {
            positions = new Vector3[LineNodeCount];
            lineR.positionCount = LineNodeCount;
        }

        Vector3 off = LineTailOffset / (LineNodeCount - 1);

        float followSpeed = deltaTime * FllowSpeed;

        for (int i = 0; i < LineNodeCount; i++)
        {
            if (i == 0)
            {
                //第一个点的坐标
                if (simulationSpace == ParticleSystemSimulationSpace.Local)
                    positions[i] = Vector3.zero;
                else
                    positions[i] = new Vector3(space.m03, space.m13, space.m23);
            }
            else
            {
                switch (simulationSpace)
                {
                    case ParticleSystemSimulationSpace.Local:
                        positions[i] = Vector3.Lerp(positions[i], positions[i - 1] + off, followSpeed);
                        break;

                    case ParticleSystemSimulationSpace.World:
                        positions[i] = Vector3.Lerp(positions[i], positions[i - 1] + space.MultiplyVector(off), followSpeed);
                        break;

                    case ParticleSystemSimulationSpace.Custom:
                        if (matCustomSpaceOffset != null)
                        {
                            //根据参考transform的移动来运动
                            positions[i] = matCustomSpaceOffset.Value.MultiplyPoint3x4(positions[i]);
                        }
                        positions[i] = Vector3.Lerp(positions[i], positions[i - 1] + space.MultiplyVector(off), followSpeed);
                        break;
                }
            }
        }
        lineR.SetPositions(positions);
    }

    //以下函数只有在custom模式下起作用
    private void SetCustomSpaceOffset()
    {
        //custom模式但没有添加参考transform, 此时相当于world模式
        if (customSimulationSpace == null)
        {
            matCustomSpaceWL = null;
            matCustomSpaceOffset = null;
            return;
        }

        //custom模式且添加了参考transform
        if (matCustomSpaceWL == null)
        {
            //第一次循环，matCustomSpaceWL没有值
            matCustomSpaceOffset = null;
        }
        else
        {
            //不是第一次循环，matCustomSpaceWL有值
            matCustomSpaceOffset = customSimulationSpace.localToWorldMatrix * matCustomSpaceWL;
        }
        matCustomSpaceWL = customSimulationSpace.worldToLocalMatrix;
    }
}
