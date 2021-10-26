using System.Linq;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Assets.MoveableLineRenderer.Scripts
{
	[ExecuteInEditMode]
	internal sealed class MoveableLineRenderer : MonoBehaviour
	{
		//public GameObject LineRendererPrefab;
		[Tooltip("默认值0代表不使用固定频率更新，更新频率跟Unity的Update保持一致")]
		[SerializeField]
		private uint _fixedTickRate = 0;
		[Header("最小间距"), Range(0.01f, 0.2f)]
		[SerializeField]
		private float MinVertexDistance = 0.1f;
		[Header("节点寿命")]
		[SerializeField]
		private float LifeTime = 1f;
		[Header("最大长度")]
		[SerializeField]
		private float MaxLength = 1f;
		[Header("计算精度(越低越准确)"), Range(1, 5)]
		[SerializeField]
		private int Resolution = 1;
		[Header("节点速度(Z方向)")]
		[SerializeField]
		private float Speed = 1f;
		[Header("重力")]
		[SerializeField]
		private float Gravity = 0f;

		//控制扰动
		//[Header("启用长度")]
		//public bool Keeplength = false;
		[Header("节点速度扰动(Z方向)")]
		public float SpeedNoise = 0f;
		[Header("振幅(X方向)")]
		public float AmplitudeX = 0f;
		[Header("振幅(Y方向)")]
		public float AmplitudeY = 0f;
		[Header("频率")]
		public float Frequency = 0f;

		//频率关联速度

		private LineRenderer _lineRenderer;
		private Point[] _points;
		private int _pointsCount;
		private float FixedMinVertexDistance;
		private float FixedLifeTime;
		private float fixedTickStep { get { return _fixedTickRate == 0 ? Time.deltaTime : 1f / (float)_fixedTickRate; } }

		private void Start()
		{
			//_lineRenderer = Instantiate(LineRendererPrefab).GetComponent<LineRenderer>();
			//_lineRenderer.enabled = true;
			//_lineRenderer.transform.parent = transform;
			//_points = new Point[100];

			if (transform.Find("MoveableLineRenderer") != null)
			{
				_lineRenderer = transform.Find("MoveableLineRenderer").gameObject.GetComponent<LineRenderer>();
			}

			else
			{
				GameObject GameObj = new GameObject();
				GameObj.name = "MoveableLineRenderer";
				GameObj.transform.parent = transform;
				_lineRenderer = GameObj.AddComponent<LineRenderer>();
				_lineRenderer.enabled = true;
				_lineRenderer.widthMultiplier = 0.5f;
				//_lineRenderer.material = new Material(Shader.Find("Standard Assets/Shaders/UnityBuiltin/Legacy/Normal-Bumped"));
			}

			_points = new Point[100];

		}

		private void Update()
		{

			if (LifeTime <= 0)
				LifeTime = 0.1f;

			RemoveOutdatedPoints();

			//修正帧率对最小间隔和lifeTime的影响。
			FixedMinVertexDistance = MinVertexDistance * fixedTickStep / Time.deltaTime;
			FixedLifeTime = LifeTime / fixedTickStep * Time.deltaTime;

			if (_pointsCount == 0)
			{
				_points[_pointsCount++] = new Point(transform.position);
				_points[_pointsCount++] = new Point(transform.position);
			}


			bool needAdd = false;

			var sqrDistance = (_points[1].Position - transform.position).sqrMagnitude;
			//需要计算尾巴长度
			if (sqrDistance > FixedMinVertexDistance * FixedMinVertexDistance)
			{
				needAdd = true;
			}

			if (needAdd)
			{
				if (_pointsCount == _points.Length)
					System.Array.Resize(ref _points, _points.Length + 50);

				InsertPoint();
			}

			ApplyTurbulence();
			Vector3[] curve = MakeSmoothCurve(_points.Where(r => r != null && r.Position != null).Select(r => r.Position).ToArray(), 3.0f);

			_lineRenderer.positionCount = curve.Length;

			//todo
			_lineRenderer.SetPositions(curve);

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
				if (point == null || point.TimeAlive >= FixedLifeTime)
				{
					_points[i] = null;
					_pointsCount--;
				}

				else
				{
					//计算是否超出最大长度值。
					if (MaxLength != 0 && MaxLength <= CurrentLengthSqr())
					{
						_points[i] = null;
						_pointsCount--;
					}
				}
			}
		}


		private void ApplyTurbulence()
		{
			//注意不要扰动到起始点，导致曲线端点位置脱离原点。
			for (int i = _pointsCount - 1; i >= 1; i--)
			{
				if (_points[i] == null)
					continue;

				var sTime = Time.timeSinceLevelLoad * Frequency;

				var pointPosition = _points[i].Position;

				float xCoord = pointPosition.x * Frequency / 100f + sTime;
				float yCoord = pointPosition.y * Frequency / 100f + sTime;
				float zCoord = pointPosition.z * Frequency / 100f + sTime;

				//_points[i].Position.x += Height;
				//_points[i].Position.y += Height - Gravity;
				//_points[i].Position.z += Height;

				//_points[i].Position.x += (Mathf.PerlinNoise(yCoord, zCoord) - 0.5f) * Speed;
				//_points[i].Position.y += (Mathf.PerlinNoise(xCoord, zCoord) - 0.5f) * Amplitude  + Height - Gravity;
				//_points[i].Position.z += (Mathf.PerlinNoise(xCoord, yCoord) - 0.5f) * Amplitude;
				// _points[i].Position.z += Speed*MinVertexDistance/LifeTime;

				//_points[i].Position += transform.localRotation * (-Vector3.forward) * Speed * MinVertexDistance / LifeTime;

				Vector3 noise = new Vector3((Mathf.PerlinNoise(yCoord, zCoord) - 0.5f) * AmplitudeX / 10f, (Mathf.PerlinNoise(xCoord, zCoord) - 0.5f) * AmplitudeY / 10f - Gravity / 100f, (Mathf.PerlinNoise(xCoord, yCoord) - 0.5f) * SpeedNoise / 10f);

				//计算时需要考虑游戏运行帧率。比如30帧对应0.0333f。暴露参数用fixedTickRate 控制。
				_points[i].Position += transform.rotation * (-Vector3.forward) * Speed * fixedTickStep + transform.rotation * noise;


			}
		}


		//计算曲线长度，根据曲线长度与最大长度的比较控制。
		private float CurrentLengthSqr()
		{
			float Lengthsqr = 0f;
			for (int i = 0; i < _pointsCount - Resolution; i = i + Resolution)
			{
				Lengthsqr = Lengthsqr + (_points[i].Position - _points[i + 1].Position).magnitude;

			}
			return Lengthsqr;
		}


		public static Vector3[] MakeSmoothCurve(Vector3[] arrayToCurve, float smoothness)
		{
			List<Vector3> points;
			List<Vector3> curvedPoints;
			int pointsLength = 0;
			int curvedLength = 0;

			if (smoothness < 1.0f) smoothness = 1.0f;

			pointsLength = arrayToCurve.Length;

			curvedLength = (pointsLength * Mathf.RoundToInt(smoothness)) - 1;
			curvedPoints = new List<Vector3>(curvedLength);

			float t = 0.0f;
			for (int pointInTimeOnCurve = 0; pointInTimeOnCurve < curvedLength + 1; pointInTimeOnCurve++)
			{
				t = Mathf.InverseLerp(0, curvedLength, pointInTimeOnCurve);

				points = new List<Vector3>(arrayToCurve);

				for (int j = pointsLength - 1; j > 0; j--)
				{
					for (int i = 0; i < j; i++)
					{
						points[i] = (1 - t) * points[i] + t * points[i + 1];
					}
				}

				curvedPoints.Add(points[0]);
			}

			return (curvedPoints.ToArray());
		}
	}
}

namespace Assets.MoveableLineRenderer.Scripts
{
	internal sealed class Point
	{
		public Vector3 Position;
		private readonly float _timeCreated;

		public Point(Vector3 position)
		{
			Position = position;
			_timeCreated = Time.time;
		}

		public float TimeAlive
		{
			get { return Time.time - _timeCreated; }
		}
	}
}


