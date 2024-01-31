using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CineControl : MonoBehaviour
{
    [SerializeField] private List<Transform> CamPos = new List<Transform>();
    [SerializeField] private List<GameObject> Props = new List<GameObject>();
    int cur;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            cur++;

            Camera.main.transform.position = CamPos[cur].position;
            Camera.main.transform.rotation = CamPos[cur].rotation;

            foreach (GameObject obj in Props)
            {
                obj.SetActive(false);
            }

            Props[cur].SetActive(true);
        }
    }
}
