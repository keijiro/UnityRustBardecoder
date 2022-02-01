using System.Runtime.InteropServices;
using IntPtr = System.IntPtr;

using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.Rendering;
using UI = UnityEngine.UI;

sealed class WebcamDecoder : MonoBehaviour
{
    [SerializeField] UI.RawImage _preview = null;
    [SerializeField] UI.Text _label = null;

    const int _decimate = 4;

    WebCamTexture _webcam;
    RenderTexture _source;

    void Start()
    {
        _webcam = new WebCamTexture(1920, 1080);
        _webcam.Play();
        _preview.texture = _webcam;
    }

    void OnDestroy()
    {
        Destroy(_webcam);
        if (_source != null) Destroy(_source);
    }

    void Update()
    {
        if (_source == null)
        {
            if (_webcam.width < 256) return; // not yet initialized

            _source = new RenderTexture
              (_webcam.width / _decimate, _webcam.height / _decimate, 0);

            _preview.GetComponent<UI.AspectRatioFitter>().aspectRatio =
              (float)_webcam.width / _webcam.height;
        }

        Graphics.Blit(_webcam, _source, new Vector2(-1, 1), new Vector2(1, 0));
        AsyncGPUReadback.Request(_source, 0, OnReadback);
    }

    unsafe void OnReadback(AsyncGPUReadbackRequest req)
    {
        if (_source == null) return;

        var image = (IntPtr)NativeArrayUnsafeUtility.
          GetUnsafeReadOnlyPtr(req.GetData<byte>(0));

        var ptr = bardecoder_decode(image, _source.width, _source.height);
        if (ptr == IntPtr.Zero) return;
        var text = Marshal.PtrToStringAnsi(ptr);

        _label.text = $"Detected: \"{text}\"";
    }

    #if !UNITY_EDITOR && (UNITY_IOS || UNITY_WEBGL)
    const string _dll = "__Internal";
    #else
    const string _dll = "bardecoder";
    #endif

    [DllImport(_dll)] static extern
      IntPtr bardecoder_decode(IntPtr image, int width, int height);
}
