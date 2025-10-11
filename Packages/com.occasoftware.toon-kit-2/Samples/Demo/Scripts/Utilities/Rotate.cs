using UnityEngine;

namespace OccaSoftware.ToonKit2.Demo
{
  public class Rotate : MonoBehaviour
  {
    public Vector3 speed = new Vector3(360f, 360f, 360f);

    private Vector3 s = Vector3.zero;

    private void OnEnable()
    {
      s.x = Random.Range(-speed.x, speed.x);
      s.y = Random.Range(-speed.y, speed.y);
      s.z = Random.Range(-speed.z, speed.z);
    }

    // Update is called once per frame
    void Update()
    {
      transform.Rotate(s * Time.deltaTime, Space.World);
    }
  }
}
