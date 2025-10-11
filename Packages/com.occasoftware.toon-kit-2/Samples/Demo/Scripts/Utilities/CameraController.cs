using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OccaSoftware.ToonKit2.Demo
{
  using UnityEngine;
  using System.Collections;

  public class CameraController : MonoBehaviour
  {
    public Transform[] positions;
    public Transform[] lookAtTargets;
    private int currentIndex = 0;

    public float transitionDuration = 1.0f;
    private float transitionStartTime;

    // Easing function for smoother interpolation
    public AnimationCurve easingCurve;

    void Start()
    {
      if (positions.Length != lookAtTargets.Length)
      {
        Debug.LogError("Positions and LookAtTargets arrays must be of the same length.");
        return;
      }

      // Set initial position and lookat target
      transform.position = positions[currentIndex].position;
      transform.LookAt(lookAtTargets[currentIndex].position);
    }

    public void NextTarget()
    {
      // Increment index and wrap around if necessary
      currentIndex = (currentIndex + 1) % positions.Length;

      // Start transition
      transitionStartTime = Time.time;

      // Move camera to the next position and lookat target
      Vector3 startPos = transform.position;
      Quaternion startRot = transform.rotation;

      Vector3 endPos = positions[currentIndex].position;
      Quaternion endRot = Quaternion.LookRotation(lookAtTargets[currentIndex].position - endPos);

      StartCoroutine(TransitionCoroutine(startPos, startRot, endPos, endRot));
    }

    private IEnumerator TransitionCoroutine(
      Vector3 startPos,
      Quaternion startRot,
      Vector3 endPos,
      Quaternion endRot
    )
    {
      float t = 0f;
      while (t < 1.0f)
      {
        t = (Time.time - transitionStartTime) / transitionDuration;
        float easedT = easingCurve.Evaluate(t); // Use easing function
        transform.position = Vector3.Lerp(startPos, endPos, easedT);
        transform.rotation = Quaternion.Lerp(startRot, endRot, easedT);
        yield return null;
      }

      // Ensure the final position and rotation are exact
      transform.position = endPos;
      transform.rotation = endRot;
    }
  }
}
