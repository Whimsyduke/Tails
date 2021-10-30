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
		[Header("默认值 0 更新频率跟Update一致")]
		[SerializeField]
		private uint _fixedTickRate = 0;
		[Header("最小间距"), Range(0.01f, 0.2f)]
		[SerializeField]
		private float MinVertexDistance = 0.1f;
		[Header("顶点插值段数"), Range(1, 10)]
		[SerializeField]
		private int Tween = 1;
		[Header("节点寿命")]
		[SerializeField]
		private float LifeTime = 1f;
		[Header("最大长度")]
		[SerializeField]
		private float MaxLength = 1f;
		[Header("节点速度(Z方向)")]
		[SerializeField]
		private float Speed = 1f;
		[Header("重力")]
		[SerializeField]
		private float Gravity = 0f;


		private enum NoiseType
		{
			None,
			PerlinNoise,
			RandomNoise,
			Sin
		}
		[Header("扰动类型")]
		[SerializeField]
		private NoiseType NoiseMode = NoiseType.None;



		[Header("节点速度扰动(Z方向)")]
		public float SpeedNoise = 0f;
		[Header("振幅(X方向)")]
		public float AmplitudeX = 0f;
		[Header("振幅(Y方向)")]
		public float AmplitudeY = 0f;
		[Header("频率")]
		public float Frequency = 0f;
		[Header("扰动渐增")]
		public float NoiseScale = 0;

		//频率关联速度

		private LineRenderer _lineRenderer;
		private List<Point> points;
		private float FixedLifeTime;
		private float fixedTickStep { get { return _fixedTickRate == 0 ? Time.fixedDeltaTime : 1f / (float)_fixedTickRate; } }
		private float timePassed;
		//高于此值的话我们认为这个时间累计是异常的，需要舍弃
		private const float TIME_PASSED_THRESHOLD = 1f;

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
			points = new List<Point>();
			timePassed = 0f;
		}

		private void FixedUpdate()
		{
			//修正帧率对最小间隔和lifeTime的影响。
			FixedLifeTime = LifeTime / fixedTickStep * Time.deltaTime;
			if (_fixedTickRate == 0)
				UpdatePoints(Time.fixedDeltaTime);
			else
				FixedStepUpdate();
		}

		/// <summary>
		/// 如果直接在Update里做，不同帧率的表现是不一样的，我们需要在逻辑上固定帧率
		/// </summary>
		private void FixedStepUpdate()
		{
			timePassed += Time.deltaTime;
			//不应该出现大于阈值的情况，如果大于阈值，就reset一波
			if (timePassed > TIME_PASSED_THRESHOLD)
			{
				timePassed = 0;
				UpdatePoints(fixedTickStep);
			}
			else
			{
				while (timePassed >= fixedTickStep)
				{
					timePassed -= fixedTickStep;
					UpdatePoints(fixedTickStep);
				}
			}
		}

		private void UpdatePoints(float deltaTime)
		{
			RemoveOutdatedPoints();

			float FixedMinVertexDistance = MinVertexDistance * fixedTickStep / Time.deltaTime;

			//需要计算尾巴长度
			if (points.Count < 2 || (points[1].Position - transform.position).sqrMagnitude > FixedMinVertexDistance * FixedMinVertexDistance)
			{
				points.Insert(0, new Point(transform.position));
			}

			float speed = Speed * fixedTickStep;

			// 节点移动
			for (int i = 1; i < points.Count; i++)
			{
				points[i].OriginPosition += transform.rotation * (-Vector3.forward) * Speed * fixedTickStep;
			}

			if (Frequency != 0)
			{
				switch (NoiseMode)
				{
					case NoiseType.None:
						ApplyNone();
						break;
					case NoiseType.PerlinNoise:
						ApplyPerlinNoise();
						break;
					case NoiseType.RandomNoise:
						ApplyRandomNoise();
						break;
					case NoiseType.Sin:
						ApplySin();
						break;
				}
			}
			if (Tween == 1)
			{
				_lineRenderer.positionCount = points.Count;
				_lineRenderer.SetPositions(points.Select(t => t.Position).ToArray());
			}
			else
			{
				Vector3[] curve = MakeSmoothCurve(points.Where(t => t != null && t.Position != null).Select(t => t.Position).ToArray(), Tween);
				_lineRenderer.positionCount = curve.Length;
				//todo
				_lineRenderer.SetPositions(curve);
			}		
		}

		public static Vector3[] MakeSmoothCurve(Vector3[] arrayToCurve, int smoothness)
		{
			List<Vector3> points;
			List<Vector3> curvedPoints;
			int pointsLength = 0;
			int curvedLength = 0;


			pointsLength = arrayToCurve.Length;

			curvedLength = (pointsLength * smoothness) - 1;
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
		private void RemoveOutdatedPoints()
		{
			if (points.Count == 0)
				return;
			//物体移动速度太快时，可能直接把点删没了导致溢出，需要注意点的数量。
			//MaxLength 设置为 0 时跳过，此时拖尾无限长度（只受寿命影响。）
			int keepCount = points.Count;
			float length = 0;
			for (int i = 0; i < points.Count; i++)
			{
				if (points[i] == null || points[i].TimeAlive >= FixedLifeTime)
				{
					keepCount = i;
					break;
				}
				if (MaxLength > 0 && i > 0)
				{
					length = length + Vector3.Distance(points[i - 1].OriginPosition, points[i].OriginPosition);
					if (length > MaxLength)
					{
						keepCount = i;
						break;
					}
				}
			}
			if  (fixedTickStep > 0.1)
				Debug.Log($"{fixedTickStep}, {Time.deltaTime}");
			points.RemoveRange(keepCount, points.Count - keepCount);
		}

		private float NoiseScaleRate(int index)
        {
			float scale = NoiseScale;
			if (scale <=2)
            {
				return 1;
            }
			return Mathf.Log(index, scale);
        }

		private void ApplyNone()
		{
			//注意不要扰动到起始点，导致曲线端点位置脱离原点。
			Vector3 noise = new Vector3(0, -Gravity / 100f, 0);
			for (int i = points.Count - 1; i >= 1; i--)
			{
				points[i].Noise +=  transform.rotation * noise;
			}

		}

		private void ApplyPerlinNoise()
		{
			//注意不要扰动到起始点，导致曲线端点位置脱离原点。
			for (int i = points.Count - 1; i >= 1; i--)
			{
				var sTime = Time.timeSinceLevelLoad * Frequency;

				var pointPosition = points[i].Position;

				float xCoord = pointPosition.x * Frequency / 100f + sTime;
				float yCoord = pointPosition.y * Frequency / 100f + sTime;
				float zCoord = pointPosition.z * Frequency / 100f + sTime;
				Vector3 noise = new Vector3((Mathf.PerlinNoise(yCoord, zCoord) - 0.5f) * AmplitudeX / 10f, (Mathf.PerlinNoise(xCoord, zCoord) - 0.5f) * AmplitudeY / 10f - Gravity / 100f, (Mathf.PerlinNoise(xCoord, yCoord) - 0.5f) * SpeedNoise / 10f);
				//计算时需要考虑游戏运行帧率。比如30帧对应0.0333f。暴露参数用fixedTickRate 控制。
				points[i].Noise += transform.rotation * noise;
			}
		}
		private void ApplyRandomNoise()
		{
			//注意不要扰动到起始点，导致曲线端点位置脱离原点。
			Vector3 noiseold = new Vector3(0, 0, 0);

			for (int i = points.Count - 1; i >= 1; i--)
			{
				Vector3 noise = new Vector3(Random.Range(-AmplitudeX / 100f, AmplitudeX / 100f), Random.Range(-AmplitudeY / 100f, AmplitudeY / 100f) - Gravity / 100f, Random.Range(-SpeedNoise / 100f, SpeedNoise / 100f));

				points[i].Noise += transform.rotation * noise - transform.rotation * noiseold;

				//每次计算下一个点时，需要把上一次计算的 noise 扣除掉，避免 noise 的累加。
				noiseold = noise;
			}
		}

		private void ApplySin()
		{
			//注意不要扰动到起始点，导致曲线端点位置脱离原点。
			float OffsetX = 0;
			float OffsetY = 0;
			float second = Time.time - Mathf.Floor(Time.time);
			float strength = Mathf.Sin(second * Frequency  * Mathf.PI * 2);
			OffsetX += AmplitudeX * strength;
			OffsetY += AmplitudeY * strength;
			Vector3 noise = new Vector3(OffsetX / 10f, OffsetY / 10f - Gravity / 100f, 0);
			//计算时需要考虑游戏运行帧率。比如30帧对应0.0333f。暴露参数用fixedTickRate 控制。

			if (points.Count > 1)
			{
				points[1].Noise = transform.rotation * noise;
				points[1].NoiseScale = NoiseScaleRate(1);
				Vector3 addNoise = points[1].Noise;
				for (int i = 2; i < points.Count; i++)
				{
					Vector3 currentNoise = points[i].Noise;
					points[i].Noise = addNoise;
					points[i].NoiseScale = NoiseScaleRate(i);
					addNoise = currentNoise;
				}
			}
		}
	}
}

namespace Assets.MoveableLineRenderer.Scripts
{
	internal sealed class Point
	{
		public Vector3 Position {
			get 
			{
				if (Noise != null)
				{
					return OriginPosition + Noise * NoiseScale;
				}
				else
				{
					return OriginPosition;
				}
			}
		}
		private readonly float _timeCreated;

		public Vector3 OriginPosition { get; set; } // 节点移动影响
		public Vector3 Noise { set; get; } // 节点扰动影响
		public float NoiseScale { set; get; } // 扰动缩放

		public Point(Vector3 position)
		{
			OriginPosition = position;
			_timeCreated = Time.time;
			NoiseScale = 1;
		}

		public float TimeAlive
		{
			get { return Time.time - _timeCreated; }
		}
	}
}


