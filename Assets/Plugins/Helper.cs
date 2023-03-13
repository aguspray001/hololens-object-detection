using SimpleJSON;

namespace HelperUtilities
{
    public abstract class Helper
    {
        public static string[] getNestedDataJsonString(JSONNode node, string name)
        {
            JSONNode data = node[name];
            string[] dataStore = new string[data.Count];

            for (int i = 0, j = data.Count - 1; i < data.Count; i++, j--)
            {
                dataStore[j] = data[i];
            }
            return dataStore;
        }

        public static float[] getNestedDataJsonFloat(JSONNode node, string name)
        {
            JSONNode data = node[name];
            float[] dataStore = new float[data.Count];

            for (int i = 0, j = data.Count - 1; i < data.Count; i++, j--)
            {
                dataStore[j] = data[i];
            }
            return dataStore;
        }

        public static float[,] getDoubleNestedDataJsonFloat(JSONNode node, string name)
        {
            JSONNode data = node[name];
            float[,] dataStore = new float[data.Count, 4];

            for (int i = 0; i < data.Count; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    dataStore[i, j] = data[i][j];
                }
            }
            return dataStore;
        }
    }
}