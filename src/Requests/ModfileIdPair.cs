namespace ModIO
{
    [System.Serializable]
    public struct ModfileIdPair
    {
        public int modId;
        public int modfileId;
    }

    [System.Serializable]
    public class FileDownloadInfo
    {
        public UnityEngine.Networking.UnityWebRequest request;
        public string target;
        public System.Int64 fileSize;
    }
}