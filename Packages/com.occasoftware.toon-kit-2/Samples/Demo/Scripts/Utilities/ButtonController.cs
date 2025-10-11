using UnityEngine;
using UnityEngine.UI;

namespace OccaSoftware.ToonKit2.Demo
{
  public class ButtonController : MonoBehaviour
  {
    public CameraController cameraController;

    void Start()
    {
      Button button = GetComponent<Button>();
      button.onClick.AddListener(NextTarget);
    }

    void NextTarget()
    {
      cameraController.NextTarget();
    }
  }
}
