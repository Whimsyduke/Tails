using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Assets.MoveableLineRenderer.Scripts
{
    internal sealed class MoveableLineRenderer : MonoBehaviour
    {
        public GameObject LineRendererPrefab;
        [Header("最小间距"), Range(0.01f, 0.2f)]
        public float MinVertexDistance = 1f;
        [Header("节点寿命")]
        public float LifeTime = 1f;
        [Header("最大长度")]
        public float MaxLength = 1f;
        [Header("计算精度(越低越准确)"), Range(1, 5)]
        public int Resolution = 1;
        [Header("节点速度(Z方向)")]
        public float Speed = 1f;
        [Header("重力")]
        public float Gravity = 0f;

        //控制扰动
        [Header("振幅")]
        public float Amplitude = 1.0f;
        [Header("频率")]
        public float Frequency = 1f;



        private LineRenderer _lineRenderer;
        private Point[] _points = { };
        private int _pointsCount = 0;

        private void Start()
        {
            _lineRenderer = Instantiate(LineRendererPrefab).GetComponent<LineRenderer>();
            _lineRenderer.enabled = true;
            _lineRenderer.transform.parent = transform;

            _points = new Point[100];
        }

        private void Update()
        {
            RemoveOutdatedPoints();

            if (_pointsCount == 0)
            {
                _points[_pointsCount++] = new Point(transform.position);
                _points[_pointsCount++] = new Point(transform.position);
            }

            bool needAdd = false;
            if (_points.Length > 0)
            {
                var sqrDistance = (_points[1].Position - transform.position).sqrMagnitude;
                if (sqrDistance > MinVertexDistance * MinVertexDistance)
                {
                    needAdd = true;
                }
            }

            if (needAdd)
            {
                if (_pointsCount == _points.Length)
                    System.Array.Resize(ref _points, _points.Length + 50);

                InsertPoint();
            }

            ApplyTurbulence();


            _lineRenderer.positionCount = _pointsCount;

            //todo
            _lineRenderer.SetPositions(_points.Where(t => t != null).Select(t => t.Position).ToArray());
        }

        private void InsertPoint()
        {
            for (int i = _pointsCount; i > 0; i--)
                _points[i] = _points[i - 1];

            _points[0] = new Point(transform.position);

            _pointsCount++;
        }

        private void RemoveOutdatedPoints()
        {
            if (_pointsCount == 0)
                return;
            //物体移动速度太快时，可能直接把点删没了导致溢出，需要注意点的数量。
            //MaxLength 设置为 0 时跳过，此时拖尾无限长度（只受寿命影响。）
            for (int i = _pointsCount - 1; i >= 2; i--)
            {
                var point = _points[i];
                if (point == null || point.TimeAlive >= LifeTime)
                {
                    _points[i] = null;
                    _pointsCount--;
                }

                else
                {
                    //计算是否超出最大长度的平方值。
                    if (MaxLength != 0 && MaxLength * MaxLength / 100 <= CurrentLengthSqr())
                    {
                        _points[i] = null;
                        _pointsCount--;
                    }
                }
            }
        }

        private void ApplyTurbulence()
        {
            for (int i = _pointsCount - 1; i >= 0; i--)
            {
                if (_points[i] == null)
                    continue;

                var sTime = Time.timeSinceLevelLoad * Frequency;

                var pointPosition = _points[i].Position;

                float xCoord = pointPosition.x * Amplitude + sTime;
                float yCoord = pointPosition.y * Amplitude + sTime;
                float zCoord = pointPosition.z * Amplitude + sTime;

                //_points[i].Position.x += Height;
                //_points[i].Position.y += Height - Gravity;
                //_points[i].Position.z += Height;

                //_points[i].Position.x += (Mathf.PerlinNoise(yCoord, zCoord) - 0.5f) * Height;
                //_points[i].Position.y += (Mathf.PerlinNoise(xCoord, zCoord) - 0.5f) * Height  + Height - Gravity;
                //_points[i].Position.z += (Mathf.PerlinNoise(xCoord, yCoord) - 0.5f) * Height;
                //_points[i].Position.z += Speed * MinVertexDistance / LifeTime;
                Vector3 noise = new Vector3((Mathf.PerlinNoise(yCoord, zCoord) - 0.5f) * Amplitude, (Mathf.PerlinNoise(xCoord, zCoord) - 0.5f) * Amplitude + Amplitude - Gravity, (Mathf.PerlinNoise(xCoord, yCoord) - 0.5f) * Amplitude);
                _points[i].Position += transform.localRotation * (-Vector3.forward) * Speed * MinVertexDistance / LifeTime + noise;
            }
        }


        //计算曲线长度，根据曲线长度与最大长度的比较控制。
        private float CurrentLengthSqr()
        {
            float Lengthsqr = 0f;
            for (int i = 0; i < _pointsCount - Resolution; i = i + Resolution)
            {
                Lengthsqr = Lengthsqr + (_points[i].Position - _points[i + 1].Position).sqrMagnitude;

            }
            return Lengthsqr;
        }


    }
}
