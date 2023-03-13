using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Barracuda;
using UnityEngine.Profiling;
//using TMPro;
//library external
using WebSocketSharp;
//library custom
using HelperUtilities;
using SimpleJSON;

[RequireComponent(typeof(OnGUICanvasRelativeDrawer))]
public class WebCamDetector : MonoBehaviour
{
    [Tooltip("File of YOLO model. If you want to use another than YOLOv2 tiny, it may be necessary to change some const values in YOLOHandler.cs")]
    public NNModel modelFile;
    [Tooltip("Text file with classes names separated by coma ','")]
    public TextAsset classesFile;

    [Tooltip("RawImage component which will be used to draw resuls.")]
    public RawImage imageRenderer;

    //[SerializeField]
    //private TextMeshProUGUI m_ClassText;
    
    [Range(0.05f, 1f)]
    [Tooltip("The minimum value of box confidence below which boxes won't be drawn.")]
    public float MinBoxConfidence = 0.3f;

    public struct ResultBox
    {
        public Rect rect;
        public float confidence;
        public float[] classes;
        public int bestClassIdx;
    }
    
    NNHandler nn;
    YOLOHandler yolo;

    WebCamTexture camTexture;
    Texture2D displayingTex;

    TextureScaler textureScaler;

    string[] classesNames;
    byte[] imageOutput = null;
    OnGUICanvasRelativeDrawer relativeDrawer;

    Color[] colorArray = new Color[] { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow };
    WebSocket ws;
    string streamURI = "ws://103.106.72.182:33002/stream";

    void Start()
    {
        ws = new WebSocket(streamURI);
        var dev = SelectCameraDevice();
        camTexture = new WebCamTexture(dev);
        camTexture.Play();

        nn = new NNHandler(modelFile);
        yolo = new YOLOHandler(nn);

        textureScaler = new TextureScaler(512, 512);

        relativeDrawer = GetComponent<OnGUICanvasRelativeDrawer>();
        relativeDrawer.relativeObject = imageRenderer.GetComponent<RectTransform>();

        classesNames = classesFile.text.Split(',');
    }

    void Update()
    {
        CaptureAndPrepareTexture(camTexture, ref displayingTex);

        //var boxes = yolo.Run(displayingTex);
        //Debug.Log(boxes);
        //DrawResults(boxes, displayingTex);
        imageRenderer.texture = displayingTex;

        //encode texture to png image
        imageOutput = displayingTex.EncodeToPNG();
        string result = System.Text.Encoding.UTF8.GetString(imageOutput);
        //Debug.Log(result);
        if (imageOutput != null)
        {
            //using (var ws = new WebSocket(streamURI))
            //{
                ws.Connect();
                ws.Send(imageOutput);

                ws.OnMessage += (sender, e) =>
                {
                    Debug.Log("Message Received from " + ((WebSocket)sender).Url + ", Data : " + e.Data);

                    JSONNode wsInfo = JSON.Parse(e.Data);

                    string[] dataClass = Helper.getNestedDataJsonString(wsInfo, "classes");
                    float[] dataConfidence = Helper.getNestedDataJsonFloat(wsInfo, "confidences");
                    float[,] dataBoxes = Helper.getDoubleNestedDataJsonFloat(wsInfo, "boxes");
                    string errorData = wsInfo["error"];
                    Debug.Log("class =>" + dataClass[0]);
                    Debug.Log("confidence =>" + dataConfidence[0]);
                    Debug.Log("data boxes =>" + dataBoxes[0, 1]);

                    //foreach (string class in dataClass)
                    //{
                    //    m_ClassText.text += class;
                    //}
                    // DrawResults(dataBoxes, displayingTex);

                };
            //}
        }
 
    }

    private void OnDestroy()
    {
        // nn.Dispose();
        // yolo.Dispose();
        textureScaler.Dispose();

        camTexture.Stop();
    }

    private void CaptureAndPrepareTexture(WebCamTexture camTexture, ref Texture2D tex)
    {
        Profiler.BeginSample("Texture processing");
        TextureCropTools.CropToSquare(camTexture, ref tex);
        textureScaler.Scale(tex);
        Profiler.EndSample();
    }

    private void DrawResults(IEnumerable<ResultBox> results, Texture2D img)
    {
        relativeDrawer.Clear();
        results.ForEach(box => DrawBox(box, displayingTex));
    }

    private void DrawBox(ResultBox box, Texture2D img)
    {
        if (box.classes[box.bestClassIdx] < MinBoxConfidence)
            return;

        TextureDrawingUtils.DrawRect(img, box.rect, colorArray[box.bestClassIdx % colorArray.Length],
                                    (int)(box.classes[box.bestClassIdx] / MinBoxConfidence), true, true);
        relativeDrawer.DrawLabel(classesNames[box.bestClassIdx], box.rect.position);
    }

    /// <summary>
    /// Return first backfaced camera name if avaible, otherwise first possible
    /// </summary>
    string SelectCameraDevice()
    {
        if (WebCamTexture.devices.Length == 0)
            throw new Exception("Any camera isn't avaible!");

        foreach (var cam in WebCamTexture.devices)
        {
            if (!cam.isFrontFacing)
                return cam.name;
        }
        return WebCamTexture.devices[0].name;
    }

}
