using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrustumCulling : MonoBehaviour
{
	private static Rect UNITRECT = new Rect(0, 0, 1, 1);

    public Camera MainCam;
	public Light ShadowLight;
    [Min(0)]
	public float ShadowDistance;
    public GameObject GO;

	private Bounds _frustumBounds = new Bounds();
	private Vector3[] _frustumPts = new Vector3[5];
    private Plane[] _frustumPlanes = new Plane[5];
    private Matrix4x4 _w2l;
    private Vector3[] _boundsPoints = new Vector3[8];

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
	    MainCam.farClipPlane = ShadowDistance;
	    QualitySettings.shadowDistance = ShadowDistance;
        FrustumW2L();
    }
    
    // 计算light space 下的视锥体
    private void FrustumW2L()
    {
        if (MainCam == null || ShadowLight == null)
            return;

        Vector3[] outCorners = new Vector3[4];
        MainCam.CalculateFrustumCorners(UNITRECT, ShadowDistance, Camera.MonoOrStereoscopicEye.Mono, outCorners);
        _frustumPts = new Vector3[]
        {
	        MainCam.transform.TransformPoint(outCorners[0]),
	        MainCam.transform.TransformPoint(outCorners[1]),
	        MainCam.transform.TransformPoint(outCorners[2]),
	        MainCam.transform.TransformPoint(outCorners[3]),
            MainCam.transform.position
        };
        _w2l = ShadowLight.transform.worldToLocalMatrix;

        Vector3 frustumPtsMax = Vector3.one * float.MinValue;
        Vector3 frustumPtsMin = Vector3.one * float.MaxValue;
        for (int i = 0; i < 5; i++)
        {
	        _frustumPts[i] = _w2l.MultiplyPoint3x4(_frustumPts[i]);
            frustumPtsMax = Vector3.Max(frustumPtsMax, _frustumPts[i]);
	        frustumPtsMin = Vector3.Min(frustumPtsMin, _frustumPts[i]);
        }

        _frustumBounds.SetMinMax(frustumPtsMin, frustumPtsMax);
    }

    // bounds 转换到 light space
    private Bounds GetLightSpaceBounds(Bounds bounds)
    {
	    GetBoundsPoints(bounds);
        Vector3 ptsMax = Vector3.one * float.MinValue;
        Vector3 ptsMin = Vector3.one * float.MaxValue;

        for (int i = 0; i < 8; i++)
        {
	        _boundsPoints[i] = _w2l.MultiplyPoint3x4(_boundsPoints[i]);
	        ptsMax = Vector3.Max(ptsMax, _boundsPoints[i]);
	        ptsMin = Vector3.Min(ptsMin, _boundsPoints[i]);
        }

        Bounds lightSpaceBounds = new Bounds();
        lightSpaceBounds.SetMinMax(ptsMin, ptsMax);
        return lightSpaceBounds;
    }

    private void GetBoundsPoints(Bounds bounds)
    {
	    _boundsPoints[0] = bounds.min;
	    _boundsPoints[1] = bounds.max;
	    _boundsPoints[2] = new Vector3(_boundsPoints[0].x, _boundsPoints[0].y, _boundsPoints[1].z);
	    _boundsPoints[3] = new Vector3(_boundsPoints[0].x, _boundsPoints[1].y, _boundsPoints[0].z);
	    _boundsPoints[4] = new Vector3(_boundsPoints[1].x, _boundsPoints[0].y, _boundsPoints[0].z);
	    _boundsPoints[5] = new Vector3(_boundsPoints[0].x, _boundsPoints[1].y, _boundsPoints[1].z);
	    _boundsPoints[6] = new Vector3(_boundsPoints[1].x, _boundsPoints[0].y, _boundsPoints[1].z);
	    _boundsPoints[7] = new Vector3(_boundsPoints[1].x, _boundsPoints[1].y, _boundsPoints[0].z);
    }

    private void OnDrawGizmos()
    {
	    Gizmos.matrix = Matrix4x4.TRS(MainCam.transform.position, MainCam.transform.rotation, new Vector3(MainCam.aspect, 1.0f, 1.0f));
	    Gizmos.DrawFrustum(Vector3.zero,MainCam.fieldOfView, MainCam.farClipPlane, MainCam.nearClipPlane, 1.0f);

	    Gizmos.color = Color.magenta;
	    Gizmos.matrix = ShadowLight.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(_frustumBounds.center, _frustumBounds.size);

        Gizmos.color = Color.green;
        for (int i = 0; i < 4; i++)
        {
	        _frustumPlanes[i] = new Plane(_frustumPts[4], _frustumPts[i], _frustumPts[(i + 1) % 4]);
            Gizmos.DrawRay((_frustumPts[4] + _frustumPts[i] + _frustumPts[(i + 1) % 4]) / 3, _frustumPlanes[i].normal);
        }
        _frustumPlanes[4] = new Plane(_frustumPts[0], _frustumPts[2], _frustumPts[1]);
        Gizmos.DrawRay((_frustumPts[0] + _frustumPts[1] + _frustumPts[2] + _frustumPts[3]) / 4, _frustumPlanes[4].normal);

        Renderer[] renderers = GO.GetComponentsInChildren<Renderer>();
        if (renderers != null)
        {
	        for (int i = 0; i < renderers.Length; i++)
	        {
		        Renderer renderer = renderers[i];
		        if (renderer != null)
		        {
			        Bounds bounds = GetLightSpaceBounds(renderer.bounds);
			        if (bounds.max.z < _frustumBounds.max.z)
				        bounds.max += Vector3.forward * (_frustumBounds.max.z - bounds.max.z + 0.001f);

                    bool show = bounds.min.z < _frustumBounds.max.z && _frustumBounds.Intersects(bounds);
			        Gizmos.color = show ? Color.green : Color.black;
			        Gizmos.DrawWireCube(bounds.center, bounds.size);
		        }
	        }
        }
    }
}
