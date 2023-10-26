using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UI = UnityEngine.UI;

public class KatuManager : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] string _weightFileName;
    [SerializeField] Texture _defaultTexture;

    [SerializeField] UI.RawImage _sourceUI;
    [SerializeField] UI.RawImage _resultUI;
    [SerializeField] UI.Text _textUI;

    [SerializeField] Shader _sobelShader;


    #endregion

    #region Internal objects

    RenderTexture _sourceTexture;
    RenderTexture _resultTexture;
    RenderTexture _edgeTexture;
    WebCamTexture _webcamTexture; // Added WebcamTexture

    Material _edgeDetectionMaterial;
    // Start is called before the first frame update
    #endregion
    #region Drawing UI implementation
    void InitializeWebcamTexture()
    {
        _webcamTexture = new WebCamTexture();
        _webcamTexture.Play();
    }
    
    void UpdateEdgeDetection()
    {
        // Apply the edge detection shader to the _sourceTexture
        RenderTexture.active = _edgeTexture;

        Graphics.Blit(_edgeTexture, _sourceTexture, _edgeDetectionMaterial);

        RenderTexture.active = null;



    }

    #endregion

    #region Pix2Pix implementation

    Dictionary<string, Pix2Pix.Tensor> _weightTable;
    Pix2Pix.Generator _generator;

    float _budget = 100;
    float _budgetAdjust = 10;

    readonly string[] _performanceLabels = {
        "N/A", "Poor", "Moderate", "Good", "Great", "Excellent"
    };

    void InitializePix2Pix()
    {
        var filePath = System.IO.Path.Combine(Application.streamingAssetsPath, _weightFileName);
        //Debug.Log(filePath);
        _weightTable = Pix2Pix.WeightReader.ReadFromFile(filePath);
        _generator = new Pix2Pix.Generator(_weightTable);
    }

    void FinalizePix2Pix()
    {
        _generator.Dispose();
        Pix2Pix.WeightReader.DisposeTable(_weightTable);
    }

    void UpdatePix2Pix()
    {
        // Advance the Pix2Pix inference until the current budget runs out.
        for (var cost = 0.0f; cost < _budget;)
        {
            if (!_generator.Running) _generator.Start(_sourceTexture);

            cost += _generator.Step();

            if (!_generator.Running) _generator.GetResult(_resultTexture);
        }

        Pix2Pix.GpuBackend.ExecuteAndClearCommandBuffer();

        // Review the budget depending on the current frame time.
        _budget -= (Time.deltaTime * 60 - 1.25f) * _budgetAdjust;
        _budget = Mathf.Clamp(_budget, 150, 1200);

        _budgetAdjust = Mathf.Max(_budgetAdjust - 0.05f, 0.5f);

        // Update the text display.
        var rate = 60 * _budget / 1000;

        var perf = (_budgetAdjust < 1) ?
            _performanceLabels[(int)Mathf.Min(5, _budget / 100)] :
            "Measuring GPU performance...";

        _textUI.text =
            string.Format("Pix2Pix refresh rate: {0:F1} Hz ({1})", rate, perf);
    }

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        // Texture initialization
        _sourceTexture = new RenderTexture(256, 256, 0);
        _resultTexture = new RenderTexture(256, 256, 0);
        _edgeTexture = new RenderTexture(256, 256, 0);

        _sourceTexture.filterMode = FilterMode.Point;
        _resultTexture.enableRandomWrite = true;
        _edgeTexture.enableRandomWrite = true;
        float _thresholdValue = 0.9f;
        _sourceTexture.Create();
        _edgeTexture.Create();
        _resultTexture.Create();

        // Initialize the webcam texture
        InitializeWebcamTexture();

        _sourceUI.texture = _sourceTexture;
        _resultUI.texture = _resultTexture;

        // Create a material for the edge detection shader
        _edgeDetectionMaterial = new Material(_sobelShader);
        _edgeDetectionMaterial.SetFloat("_Threshold", _thresholdValue);
       
        InitializePix2Pix();
    }

    void OnDestroy()
    {
        Destroy(_sourceTexture);
        Destroy(_resultTexture);
        Destroy(_edgeTexture);
        Destroy(_edgeDetectionMaterial);

        FinalizePix2Pix();
    }

    void Update()
    {

        if (_webcamTexture != null)
        {
            Graphics.Blit(_webcamTexture, _edgeTexture);
            UpdateEdgeDetection();
        }
      

        UpdatePix2Pix();
    }
    #endregion
}
