using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;

namespace ModIO
{
    /// <summary>An interface for storing/loading mod.io data on disk.</summary>
    public static class DataStorage
    {
        // ---------[ Nested Data-Types ]---------
        /// <summary>Delegate for the initialization callback.</summary>
        public delegate void InitializationCallback();

        /// <summary>Delegate for the read file callback.</summary>
        public delegate void ReadFileCallback(bool success, byte[] data);

        /// <summary>Delegate for the read json file callback.</summary>
        public delegate void ReadJsonFileCallback<T>(bool success, T jsonObject);

        /// <summary>Delegate for write/delete file callbacks.</summary>
        public delegate void WriteFileCallback(bool success);

        /// <summary>The collection of platform specific functions.</summary>
        public struct PlatformFunctions
        {
            // --- Delegates ---
            /// <summary>Delegate for initializing the storage system.</summary>
            public delegate void InitializationDelegate(InitializationCallback callback);

            /// <summary>Delegate for reading a file.</summary>
            public delegate void ReadFileDelegate(string filePath, ReadFileCallback callback);

            /// <summary>Delegate for writing a file.</summary>
            public delegate void WriteFileDelegate(string filePath, byte[] fileData, WriteFileCallback callback);

            /// <summary>Delegate for deleting a file.</summary>
            public delegate void DeleteFileDelegate(string filePath, WriteFileCallback callback);

            /// <summary>Delegate for clearing all data.</summary>
            public delegate void ClearAllDataDelegate(WriteFileCallback callback);

            // --- Fields ---
            /// <summary>Delegate for initializing the storage system.</summary>
            public InitializationDelegate Initialize;

            /// <summary>Delegate for reading a file.</summary>
            public ReadFileDelegate ReadFile;

            /// <summary>Delegate for writing a file.</summary>
            public WriteFileDelegate WriteFile;

            /// <summary>Delegate for deleting a file.</summary>
            public DeleteFileDelegate DeleteFile;

            /// <summary>Delegate for clearing all data.</summary>
            public ClearAllDataDelegate ClearAllData;
        }

        // ---------[ Constants ]---------
        /// <summary>Defines the i/o functions to use for this platform.</summary>
        public static readonly PlatformFunctions PLATFORM;

        // ---------[ Fields ]---------
        /// <summary>Has DataStorage been initialized?</summary>
        public static bool isInitialized = false;

        // ---------[ Initialization ]---------
        /// <summary>Loads the platform I/O behaviour.</summary>
        static DataStorage()
        {
            #if true
                DataStorage.PLATFORM = DataStorage.GetPlatformFunctions_Standalone();
            #endif
        }

        // ---------[ Platform I/O ]---------
        #if true // --- Standalone I/O ---

            /// <summary>Returns the platform specific functions. (Standalone Application)</summary>
            public static PlatformFunctions GetPlatformFunctions_Standalone()
            {
                return new PlatformFunctions()
                {
                    Initialize = Initialize_Standalone,
                    ReadFile = ReadFile_Standalone,
                    // WriteFile = WriteFile_Standalone,
                    // DeleteFile = DeleteFile_Standalone,
                    // ClearAllData = ClearAllData_Standalone,
                };
            }

            /// <summary>Initializes the data storage system. (Standalone Application)</summary>
            public static void Initialize_Standalone(InitializationCallback callback)
            {
                Debug.Log("[mod.io] DataStorage successfully initialized.");

                DataStorage.isInitialized = true;

                if(callback != null)
                {
                    callback.Invoke();
                }
            }

            /// <summary>Reads a data file. (Standalone Application)</summary>
            public static void ReadFile_Standalone(string filePath, ReadFileCallback callback)
            {
                Debug.Assert(!string.IsNullOrEmpty(filePath));
                Debug.Assert(callback != null);

                byte[] data = null;
                bool success = false;

                if(File.Exists(filePath))
                {
                    try
                    {
                        data = File.ReadAllBytes(filePath);
                        success = true;
                    }
                    catch(Exception e)
                    {
                        data = null;

                        string warningInfo = ("[mod.io] Failed to read file.\nFile: " + filePath + "\n\n");

                        Debug.LogWarning(warningInfo
                                         + Utility.GenerateExceptionDebugString(e));
                    }
                }

                callback.Invoke(success, data);
            }
        #endif
    }
}
