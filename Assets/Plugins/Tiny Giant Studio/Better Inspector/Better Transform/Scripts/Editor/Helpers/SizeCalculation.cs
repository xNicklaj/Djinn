using UnityEngine;
using static TinyGiantStudio.BetterInspector.BetterMath;

namespace TinyGiantStudio.BetterInspector
{
    public class SizeCalculation
    {
        readonly BetterTransformSettings _editorSettings;

        public SizeCalculation(BetterTransformSettings editorSettings)
        {
            _editorSettings = editorSettings;
        }

        public Bounds GetSelfBounds(Transform target)
        {
            if (_editorSettings.CurrentSizeType == BetterTransformSettings.SizeType.Renderer)
            {
                if (target.GetComponent<Renderer>() == null)
                    return new(Vector3.zero, Vector3.zero);

                if (_editorSettings.CurrentWorkSpace == BetterTransformSettings.WorkSpace.World)
                {
                    Bounds bounds = target.GetComponent<Renderer>().bounds;
                    bounds.center -= target.position;

                    return bounds;
                }
                else
                {
#if UNITY_2021_1_OR_NEWER
                    Bounds bounds = target.GetComponent<Renderer>().localBounds;
#else
                    Bounds bounds = target.GetComponent<Renderer>().bounds; //todo
#endif
                    bounds.size = Multiply(target.lossyScale, bounds.size);
                    bounds.center = Multiply(target.lossyScale, bounds.center);

                    return bounds;
                }
            }

            MeshFilter meshFilter = target.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                if (!meshFilter.sharedMesh)
                    return new(Vector3.zero, Vector3.zero);

                Bounds bounds = meshFilter.sharedMesh.bounds;
                bounds.size = Multiply(bounds.size, target.lossyScale);

                return bounds;
            }

            if (!target.GetComponent<RectTransform>()) return new(Vector3.zero, Vector3.zero);
            Vector3[] v = new Vector3[4];
            target.GetComponent<RectTransform>().GetLocalCorners(v);

            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            foreach (Vector3 vector3 in v)
            {
                min = Vector3.Min(min, vector3);
                max = Vector3.Max(max, vector3);
            }

            Bounds newBounds = new();
            newBounds.SetMinMax(min, max);
            return newBounds;
        }
    }
}