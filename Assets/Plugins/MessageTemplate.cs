using UnityEngine;


namespace Message
{
    [SerializeField]
    public class PredictResponse
    {
        public string[] classes;
        public float[] confidences;
        public float[,] box;
        public string error;
    }
}