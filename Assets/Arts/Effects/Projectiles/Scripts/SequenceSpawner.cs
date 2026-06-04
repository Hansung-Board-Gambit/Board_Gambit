using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StylizedProjectilesAndMagicFX
{
	public class SequenceSpawner : MonoBehaviour
	{
		[Header("Settings")]
		public List<GameObject> prefabsToSpawn;
		public Transform startPoint;
		public Transform endPoint;
		public float moveDuration = 2.5f;

		private int currentIndex = 0;

		private void Start()
		{
			if (prefabsToSpawn != null && prefabsToSpawn.Count > 0 && startPoint != null && endPoint != null)
			{
				StartCoroutine(SpawnAndMoveRoutine());
			}
			else
			{
				Debug.LogWarning("SequenceSpawner: Missing references or empty list!");
			}
		}

		private IEnumerator SpawnAndMoveRoutine()
		{
			while (true)
			{
				if (prefabsToSpawn[currentIndex] == null)
				{
					yield return null;
					continue;
				}

				GameObject currentObj = Instantiate(prefabsToSpawn[currentIndex], startPoint.position, startPoint.rotation);

				float elapsedTime = 0f;
				Vector3 startingPos = startPoint.position;
				Vector3 targetPos = endPoint.position;

				while (elapsedTime < moveDuration)
				{
					if (currentObj == null) break;

					float t = elapsedTime / moveDuration;
					currentObj.transform.position = Vector3.Lerp(startingPos, targetPos, t);

					elapsedTime += Time.deltaTime;
					yield return null;
				}

				if (currentObj != null)
				{
					currentObj.transform.position = targetPos;
					Destroy(currentObj);
				}

				currentIndex = (currentIndex + 1) % prefabsToSpawn.Count;
			}
		}
	}
}