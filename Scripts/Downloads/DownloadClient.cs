using System;
using System.IO;

using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    public static class DownloadClient
    {
        // ---------[ IMAGE DOWNLOADS ]---------
        public static ImageRequest DownloadModLogo(ModProfile profile, LogoVersion version)
        {
            ImageRequest request = new ImageRequest();

            string logoURL = profile.logoLocator.GetVersionURL(version);

            UnityWebRequest webRequest = UnityWebRequest.Get(logoURL);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            var operation = webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnImageDownloadCompleted(operation, request);

            return request;
        }

        public static ImageRequest DownloadModGalleryImage(ModProfile profile,
                                                            string imageFileName,
                                                            ModGalleryImageVersion version)
        {
            ImageRequest request = new ImageRequest();

            string imageURL = profile.media.GetGalleryImageWithFileName(imageFileName).GetVersionURL(version);

            UnityWebRequest webRequest = UnityWebRequest.Get(imageURL);
            webRequest.downloadHandler = new DownloadHandlerTexture(true);

            var operation = webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnImageDownloadCompleted(operation, request);

            return request;
        }

        private static void OnImageDownloadCompleted(UnityWebRequestAsyncOperation operation,
                                                     ImageRequest request)
        {
            UnityWebRequest webRequest = operation.webRequest;

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                WebRequestError error = WebRequestError.GenerateFromWebRequest(webRequest);
                request.NotifyFailed(error);
            }
            else
            {
                #if DEBUG
                if(GlobalSettings.LOG_ALL_WEBREQUESTS)
                {
                    var responseTimeStamp = ServerTimeStamp.Now;
                    Debug.Log(String.Format("{0} REQUEST SUCEEDED\nResponse received at: {1} [{2}]\nURL: {3}\nResponse: {4}\n",
                                            webRequest.method.ToUpper(),
                                            ServerTimeStamp.ToLocalDateTime(responseTimeStamp),
                                            responseTimeStamp,
                                            webRequest.url,
                                            webRequest.downloadHandler.text));
                }
                #endif

                Texture2D imageTexture = (webRequest.downloadHandler as DownloadHandlerTexture).texture;
                request.NotifySucceeded(imageTexture);
            }
        }

        // ---------[ BINARY DOWNLOADS ]---------
        public static ModBinaryRequest DownloadModBinary(int modId, int modfileId)
        {
            ModBinaryRequest request = new ModBinaryRequest();

            request.isDone = false;

            // - Acquire Download URL -
            APIClient.GetModfile(modId, modfileId,
                                 (mf) => DownloadClient.OnGetModfile(mf, request),
                                 request.NotifyFailed);

            return request;
        }

        private static void OnGetModfile(Modfile modfile, ModBinaryRequest request)
        {
            CacheClient.SaveModfile(modfile);

            string filePath = CacheClient.GenerateModBinaryZipFilePath(modfile.modId, modfile.id);
            string tempFilePath = filePath + ".download";

            request.webRequest = UnityWebRequest.Get(modfile.downloadLocator.binaryURL);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath));
                request.webRequest.downloadHandler = new DownloadHandlerFile(tempFilePath);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to create download file on disk."
                      + "\nFile: " + tempFilePath + "\n");

                Utility.LogExceptionAsWarning(warningInfo, e);

                request.NotifyFailed(new WebRequestError());

                return;
            }

            var operation = request.webRequest.SendWebRequest();
            operation.completed += (o) => DownloadClient.OnModBinaryRequestCompleted(operation,
                                                                                     request,
                                                                                     filePath);
        }

        private static void OnModBinaryRequestCompleted(UnityWebRequestAsyncOperation operation,
                                                        ModBinaryRequest request,
                                                        string filePath)
        {
            UnityWebRequest webRequest = operation.webRequest;
            request.isDone = true;
            request.filePath = null;

            if(webRequest.isNetworkError || webRequest.isHttpError)
            {
                WebRequestError error = WebRequestError.GenerateFromWebRequest(webRequest);

                request.NotifyFailed(error);
            }
            else
            {
                #if DEBUG
                if(GlobalSettings.LOG_ALL_WEBREQUESTS)
                {
                    var responseTimeStamp = ServerTimeStamp.Now;
                    Debug.Log("DOWNLOAD SUCEEDED"
                              + "\nDownload completed at: " + ServerTimeStamp.ToLocalDateTime(responseTimeStamp)
                              + "\nURL: " + webRequest.url
                              + "\nFilePath: " + filePath);
                }
                #endif

                try
                {
                    if(File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    File.Move(filePath + ".download", filePath);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to save mod binary."
                                          + "\nFile: " + filePath + "\n");

                    Utility.LogExceptionAsWarning(warningInfo, e);

                    request.NotifyFailed(new WebRequestError());
                }

                request.filePath = filePath;

                request.NotifySucceeded();
            }
        }
    }
}
