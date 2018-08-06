﻿// #define DO_NOT_LOAD_CACHE

using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;

namespace ModIO
{
    /// <summary>An interface for storing/loading data retrieved for the mod.io servers on disk.</summary>
    /// <para>This class is the core interface for interacting with data saved to disk. It can be
    /// used directly for manual, fine-grained control, or managed via [[ModIO.ModManager]] for a
    /// hands-off, simpler approach.</para>
    public static class CacheClient
    {
        // ---------[ MEMBERS ]---------
        /// <summary>Directory for the cache.</summary>
        /// <para>Access to this variable is achieved through
        /// [[ModIO.CacheClient.TrySetCacheDirectory]] and [[ModIO.CacheClient.GetCacheDirectory]].</para>
        private static string _cacheDirectory = null;

        // ---------[ INITIALIZATION ]---------
        static CacheClient()
        {
            string dir;
            #pragma warning disable 0162
            #if DEBUG
            if(GlobalSettings.USE_TEST_SERVER)
            {
                dir = Application.persistentDataPath + "/modio_testServer/";
            }
            else
            #endif
            {
                dir = Application.persistentDataPath + "/modio/";
            }
            #pragma warning restore 0162

            TrySetCacheDirectory(dir);
        }


        /// <summary>Attempts to set the cache directory.</summary>
        /// <para>Calling this will create the directory specified if it does not exist.</para>
        /// <param name="directory">The absolute or relative directory to use</param>
        /// <returns>True if successful</returns>
        public static bool TrySetCacheDirectory(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);

                Debug.Log("[mod.io] Successfully set cache directory to  " + directory);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to set cache directory."
                                      + "\nDirectory: " + directory + "\n\n");

                Debug.LogError(warningInfo
                               + Utility.GenerateExceptionDebugString(e));

                return false;
            }

            CacheClient._cacheDirectory = directory;
            return true;
        }

        // ---------[ GET DIRECTORIES ]---------
        /// <summary>Retrieves the directory the CacheClient uses.</summary>
        public static string GetCacheDirectory()
        {
            return CacheClient._cacheDirectory;
        }

        /// <summary>Generates the path for a mod cache directory.</summary>
        /// <para>This directory will be a sub-directory of the current
        /// [cache directory](ModIO.CacheClient._cacheDirectory) and will contain all of the cached
        /// data for the given mod.</para>
        /// <param name="modId">Mod to generate the cache directory path for.</param>
        public static string GenerateModDirectoryPath(int modId)
        {
            return(CacheClient._cacheDirectory
                   + "mods/"
                   + modId.ToString() + "/");
        }

        /// <summary>[Obsolete] Generates the path for a cached mod build directory.</summary>
        [Obsolete("Use CacheClient.GenerateModBinariesDirectoryPath() instead.")]
        public static string GenerateModBuildsDirectoryPath(int modId)
        { return CacheClient.GenerateModBinariesDirectoryPath(modId); }

        /// <summary>Generates the path for a cached mod build directory.</summary>
        /// <para>This directory will be a sub-directory of the current
        /// [cache directory](ModIO.CacheClient._cacheDirectory) and will act as the target
        /// directory for any downloaded mod binaries.</para>
        /// <param name="modId">Mod to generate the binary directory path for.</param>
        public static string GenerateModBinariesDirectoryPath(int modId)
        {
            return(CacheClient.GenerateModDirectoryPath(modId)
                   + "binaries/");
        }


        // ---------[ BASIC FILE I/O ]---------
        /// <summary>Reads an entire file and parses the JSON Object it contains.</summary>
        /// <para>This function is a simple wrapper for reading text from a file and deserializing
        /// it, but handles any exceptions, logging a debug warning if the read and/or
        /// deserialization fails.</para>
        /// <param name="filePath">Location of the file to be read.</param>
        /// <returns>A new object containing the values parsed from the file. If the read or
        /// deserialization fails, this function returns `null` for objects or the default value for
        /// simple types.</returns>
        public static T ReadJsonObjectFile<T>(string filePath)
        {
            if(File.Exists(filePath))
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read json object from file."
                                          + "\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));
                }
            }
            return default(T);
        }

        /// <summary>Writes an object to a file in the JSON Object format.</summary>
        /// <para>This function is a simple wrapper for serializing an object and writing text to a
        /// file but handles any exceptions, logging a debug warning if the serialization and/or
        /// write fails.</para>
        /// <param name="filePath">Location of the file to be written. (Created if non-existent.)</param>
        /// <param name="jsonObject">Object to serialize.</param>
        public static void WriteJsonObjectFile<T>(string filePath,
                                                  T jsonObject)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, JsonConvert.SerializeObject(jsonObject));
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to write json object to file."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }
        }

        /// <summary>Loads an entire binary file as a byte array.</summary>
        /// <para>This function is a simple wrapper for
        /// <a href="https://msdn.microsoft.com/en-us/library/system.io.file.readallbytes(v=vs.110).aspx">File.ReadAllBytes</a>,
        /// but handles any exceptions, logging a debug warning if the read fails. It loads the
        /// entire binary file synchronously, and as such blocks the thread until the read is
        /// completed. Thus it is recommended that this function is only used for reading smaller
        /// files.</para>
        /// <param name="filePath">Location of the file to be read.</param>
        /// <returns>The data read from the binary file. If the read failes, this function returns
        /// `null`.</returns>
        public static byte[] LoadBinaryFile(string filePath)
        {
            byte[] fileData = null;

            if(File.Exists(filePath))
            {
                try
                {
                    fileData = File.ReadAllBytes(filePath);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read binary file."
                                          + "\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    fileData = null;
                }
            }

            return fileData;
        }

        /// <summary>Writes an entire binary file.</summary>
        /// <para>This function is a simple wrapper for
        /// <a href="https://msdn.microsoft.com/en-us/library/system.io.file.writeallbytes(v=vs.110).aspx">File.WriteAllBytes</a>,
        /// but handles any exceptions, logging a debug warning if the write fails. It writes the
        /// entire binary file synchronously, and as such blocks the thread until the write is
        /// completed. Thus it is recommended that this function is only used for writing smaller
        /// files.</para>
        /// <param name="filePath">Location of the file to be written. (Created if non-existent.)</param>
        /// <param name="data">Data to write.</param>
        public static void WriteBinaryFile(string filePath,
                                           byte[] data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, data);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to write binary file."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }
        }

        /// <summary>Loads the image data from a file into a new Texture.</summary>
        /// <para>This function is a simple wrapper for reading image data from a file and creating
        /// a texture for it, but handles any exceptions, logging a debug warning if the read fails.</para>
        /// <param name="filePath">Location of the image file to load.</param>
        /// <returns>A new
        /// <a href="https://docs.unity3d.com/ScriptReference/Texture2D.html">Texture2D</a> object
        /// containing the image data parsed from the file. If the read fails, this function returns
        /// `null`.</returns>
        public static Texture2D ReadImageFile(string filePath)
        {
            Texture2D texture = null;

            if(File.Exists(filePath))
            {
                byte[] imageData = CacheClient.LoadBinaryFile(filePath);

                if(imageData != null)
                {
                    texture = new Texture2D(0,0);
                    texture.LoadImage(imageData);
                }
            }

            return texture;
        }

        /// <summary>Writes a texture to a PNG file.</summary>
        /// <para>This function is a simple wrapper for writing image data to a file, but handles
        /// any exceptions, logging a debug warning if the read fails.</para>
        /// <param name="filePath">Location of the file to be written. (Created if non-existent.)</param>
        /// <param name="data">Texture containing the image data to be written.</param>
        public static void WritePNGFile(string filePath,
                                        Texture2D texture)
        {
            Debug.Assert(Path.GetExtension(filePath).Equals(".png"),
                         "[mod.io] Images can only be saved in PNG format."
                         + "\n" + filePath
                         + " is an invalid file path.");

            CacheClient.WriteBinaryFile(filePath,
                                        texture.EncodeToPNG());
        }

        /// <summary>Deletes a file.</summary>
        /// <para>This function is a simple wrapper for
        /// <a href="https://msdn.microsoft.com/en-us/library/system.io.file.delete(v=vs.110).aspx">File.Delete</a>,
        /// but handles any exceptions, logging a debug warning if the delete fails.</para>
        /// <param name="filePath">Location of the file to be deleted.</param>
        public static void DeleteFile(string filePath)
        {
            try
            {
                if(File.Exists(filePath)) { File.Delete(filePath); }
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete file."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }
        }

        /// <summary>Deletes a directory.</summary>
        /// <para>This function is a simple wrapper for
        /// <a href="https://msdn.microsoft.com/en-us/library/system.io.directory.delete(v=vs.110).aspx">Directory.Delete</a>,
        /// but handles any exceptions, logging a debug warning if the delete fails.</para>
        /// <param name="directoryPath">Location of the directory to be deleted.</param>
        public static void DeleteDirectory(string directoryPath)
        {
            try
            {
                if(Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete directory."
                                      + "\nDirectory: " + directoryPath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }
        }


        // ---------[ AUTHENTICATED USER ]---------
        /// <summary>Wrapper object for managing the authenticated user's information.</summary>
        [Serializable]
        private class AuthenticatedUser
        {
            public string oAuthToken;
            public int userId;
            public List<int> modIds;
            public List<int> subscribedModIds;
        }

        /// <summary>File path for the authenticated user data.</summary>
        /// <para>All of authented user's data, except the user's profile, will be stored in this
        /// single data file under the cache directory.</para>
        public static string userFilePath
        { get { return CacheClient._cacheDirectory + "user.data"; } }

        /// <summary>Stores the authenticated user token in the cache.</summary>
        /// <para>See also: [[ModIO.APIClient.GetOAuthToken]]</para>
        /// <param name="oAuthToken">User Authentication Token to store</param>
        public static void SaveAuthenticatedUserToken(string oAuthToken)
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au == null)
            {
                au = new AuthenticatedUser();
            }

            au.oAuthToken = oAuthToken;

            CacheClient.WriteJsonObjectFile(userFilePath, au);
        }

        /// <summary>Retrieves the authenticated user token from the cache.</summary>
        /// <returns>Stored User Authentication token</returns>
        public static string LoadAuthenticatedUserToken()
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au != null)
            {
                return au.oAuthToken;
            }
            return null;
        }

        /// <summary>Stores the authenticated user's profile in the cache.</summary>
        /// <param name="userProfile">User profile retrieved via [[ModIO.APIClient.GetAuthenticatedUser]]</param>
        public static void SaveAuthenticatedUserProfile(UserProfile userProfile)
        {
            CacheClient.SaveUserProfile(userProfile);

            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au == null)
            {
                au = new AuthenticatedUser();
            }

            au.userId = userProfile.id;

            CacheClient.WriteJsonObjectFile(userFilePath, au);
        }

        /// <summary>Retrieves the authenticated user's profile from the cache.</summary>
        /// <returns>Stored user profile</returns>
        public static UserProfile LoadAuthenticatedUserProfile()
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au != null
               && au.userId > 0)
            {
                return LoadUserProfile(au.userId);
            }
            return null;
        }

        /// <summary>Stores the authenticated user's mod subscriptions in the cache.</summary>
        /// <param name="subscribedModIds">Ids of the mods retrieved via
        /// [[ModIO.APIClient.GetUserSubscriptions]]</param>
        public static void SaveAuthenticatedUserSubscriptions(List<int> subscribedModIds)
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au == null)
            {
                au = new AuthenticatedUser();
            }

            au.subscribedModIds = subscribedModIds;

            CacheClient.WriteJsonObjectFile(userFilePath, au);
        }

        /// <summary>Retrieves the authenticated user's mod subscriptions from the cache.</summary>
        /// <returns>Ids of the mods the user is subscribed to</returns>
        public static List<int> LoadAuthenticatedUserSubscriptions()
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au != null)
            {
                return au.subscribedModIds;
            }
            return null;
        }

        /// <summary>Stores the authenticated user's mods in the cache.</summary>
        /// <param name="modIds">Ids of the mods retrieved via
        /// [[ModIO.APIClient.GetUserMods]]</param>
        public static void SaveAuthenticatedUserMods(List<int> modIds)
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au == null)
            {
                au = new AuthenticatedUser();
            }

            au.modIds = modIds;

            CacheClient.WriteJsonObjectFile(userFilePath, au);
        }

        /// <summary>Retrieves the authenticated user's mods from the cache.</summary>
        /// <returns>Ids for the mods the user is a team member of</returns>
        public static List<int> LoadAuthenticatedUserMods()
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au != null)
            {
                return au.modIds;
            }

            return null;
        }

        /// <summary>Deletes the authenticated user's data from the cache.</summary>
        /// <para>This clears out all of the authentication data, the user's subscription data, and
        /// the user's mods. The user's profile is kept cached along but can only be accessed via
        /// [[ModIO.CacheClient.LoadUserProfile]].</para>
        public static void DeleteAuthenticatedUser()
        {
            try
            {
                if(File.Exists(userFilePath)) { File.Delete(userFilePath); }
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete user data save file."
                                      + "\nFile: " + userFilePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }
        }


        // ---------[ GAME PROFILE ]---------
        /// <summary>File path for the game profile data.</summary>
        public static string gameProfileFilePath
        { get { return CacheClient._cacheDirectory + "game_profile.data"; } }

        /// <summary>Stores the game's profile in the cache.</summary>
        /// <param name="profile">Game Profile to store</param>
        public static void SaveGameProfile(GameProfile profile)
        {
            CacheClient.WriteJsonObjectFile(gameProfileFilePath, profile);
        }

        /// <summary>Retrieves the game's profile from the cache.</summary>
        /// <returns>Game Profile stored via [[ModIO.CacheClient.SaveGameProfile]].</returns>
        public static GameProfile LoadGameProfile()
        {
            return CacheClient.ReadJsonObjectFile<GameProfile>(gameProfileFilePath);
        }


        // ---------[ MOD PROFILES ]---------
        /// <summary>Generates the file path for a mod's profile data.</summary>
        /// <param name="modId">Mod to generate the file path for</param>
        /// <returns>Location of the file that contains the mod profile</returns>
        public static string GenerateModProfileFilePath(int modId)
        {
            return (CacheClient.GenerateModDirectoryPath(modId)
                    + "profile.data");
        }

        /// <summary>Stores a mod's profile in the cache.</summary>
        /// <para>See also: [[ModIO.CacheClient.LoadModProfile]]</para>
        /// <param name="profile">Mod profile to store</param>
        public static void SaveModProfile(ModProfile profile)
        {
            Debug.Assert(profile.id > 0,
                         "[mod.io] Cannot cache a mod without a mod id");

            CacheClient.WriteJsonObjectFile(GenerateModProfileFilePath(profile.id),
                                             profile);
        }

        /// <summary>Retrieves a mod's profile from the cache.</summary>
        /// <para>See also: [[ModIO.CacheClient.SaveModProfile]]</para>
        /// <param name="modId">Mod profile to retrieve</param>
        /// <returns>Mod profile stored in the cache for the given mod id.</returns>
        public static ModProfile LoadModProfile(int modId)
        {
            string profileFilePath = GenerateModProfileFilePath(modId);
            ModProfile profile = CacheClient.ReadJsonObjectFile<ModProfile>(profileFilePath);
            return(profile);
        }

        /// <summary>Stores a collection of mod profiles in the cache.</summary>
        /// <para>See also: [[ModIO.CacheClient.SaveModProfile]],
        /// [[ModIO.CacheClient.IterateAllModProfiles]]</para>
        /// <param name="modProfiles">Enumeration of mods to store</param>
        public static void SaveModProfiles(IEnumerable<ModProfile> modProfiles)
        {
            foreach(ModProfile profile in modProfiles)
            {
                CacheClient.SaveModProfile(profile);
            }
        }

        /// <summary>[Obsolete] Iterates through all of the mod profiles in the cache.</summary>
        /// <para>Use [[ModIO.CacheClient.IterateAllModProfiles]] instead.</para>
        [Obsolete("Use CacheClient.IterateAllModProfiles() instead.")]
        public static IEnumerable<ModProfile> AllModProfiles()
        { return CacheClient.IterateAllModProfiles(); }

        /// <summary>Iterates through all of the mod profiles in the cache.</summary>
        /// <returns>An enumeration of all the mod profiles currently stored in the cache.</returns>
        public static IEnumerable<ModProfile> IterateAllModProfiles()
        {
            string profileDirectory = CacheClient._cacheDirectory + "mods/";

            if(Directory.Exists(profileDirectory))
            {
                string[] modDirectories;
                try
                {
                    modDirectories = Directory.GetDirectories(profileDirectory);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read mod profile directory."
                                          + "\nDirectory: " + profileDirectory + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    modDirectories = new string[0];
                }

                foreach(string modDirectory in modDirectories)
                {
                    ModProfile profile = CacheClient.ReadJsonObjectFile<ModProfile>(modDirectory + "/profile.data");

                    if(profile != null)
                    {
                        yield return profile;
                    }
                }
            }
        }

        /// <summary>Deletes all of a mod's data from the cache.</summary>
        /// <para>Removes all of the mod's cached data from the disk - the profile, binaries, and
        /// any imagery.</para>
        /// <param name="modId">Mod to delete the data for</param>
        public static void DeleteMod(int modId)
        {
            string modDir = CacheClient.GenerateModDirectoryPath(modId);
            CacheClient.DeleteDirectory(modDir);
        }

        // ---------[ MODFILES ]---------
        /// <summary>Generates the file path for a modfile.</summary>
        public static string GenerateModfileFilePath(int modId, int modfileId)
        {
            return(CacheClient.GenerateModBinariesDirectoryPath(modId)
                   + modfileId + ".data");
        }

        /// <summary>Generates the file path for a mod binary.</summary>
        public static string GenerateModBinaryZipFilePath(int modId, int modfileId)
        {
            return(CacheClient.GenerateModBinariesDirectoryPath(modId)
                   + modfileId + ".zip");
        }

        /// <summary>Stores a modfile in the cache.</summary>
        public static void SaveModfile(Modfile modfile)
        {
            Debug.Assert(modfile.modId > 0,
                         "[mod.io] Cannot cache a modfile without a mod id");
            Debug.Assert(modfile.id > 0,
                         "[mod.io] Cannot cache a modfile without a modfile id");

            CacheClient.WriteJsonObjectFile(GenerateModfileFilePath(modfile.modId, modfile.id),
                                            modfile);
        }

        /// <summary>Retrieves a modfile from the cache.</summary>
        public static Modfile LoadModfile(int modId, int modfileId)
        {
            string modfileFilePath = GenerateModfileFilePath(modId, modfileId);
            var modfile = CacheClient.ReadJsonObjectFile<Modfile>(modfileFilePath);
            return modfile;
        }

        /// <summary>Stores a mod binary's ZipFile data in the cache.</summary>
        public static void SaveModBinaryZip(int modId, int modfileId,
                                            byte[] modBinary)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod binary without a mod id");
            Debug.Assert(modfileId > 0,
                         "[mod.io] Cannot cache a mod binary without a modfile id");

            string filePath = GenerateModBinaryZipFilePath(modId, modfileId);
            CacheClient.WriteBinaryFile(filePath, modBinary);
        }

        /// <summary>Retrieves a mod binary's ZipFile data from the cache.</summary>
        public static byte[] LoadModBinaryZip(int modId, int modfileId)
        {
            string filePath = GenerateModBinaryZipFilePath(modId, modfileId);
            byte[] zipData = CacheClient.LoadBinaryFile(filePath);
            return zipData;
        }

        /// <summary>Deletes a modfile and binary from the cache.</summary>
        public static void DeleteModfileAndBinaryZip(int modId, int modfileId)
        {
            CacheClient.DeleteFile(CacheClient.GenerateModfileFilePath(modId, modfileId));
            CacheClient.DeleteFile(CacheClient.GenerateModBinaryZipFilePath(modId, modfileId));
        }

        // ---------[ MOD MEDIA ]---------
        /// <summary>Generates the directory path for a mod logo collection.</summary>
        public static string GenerateModLogoCollectionDirectoryPath(int modId)
        {
            return(CacheClient.GenerateModDirectoryPath(modId)
                   + "logo/");
        }

        /// <summary>Generates the file path for a mod logo.</summary>
        public static string GenerateModLogoFilePath(int modId, LogoSize size)
        {
            return (GenerateModLogoCollectionDirectoryPath(modId)
                    + size.ToString() + ".png");
        }

        /// <summary>Generates the file path for a mod logo's cached version information.</summary>
        public static string GenerateModLogoVersionInfoFilePath(int modId)
        {
            return(CacheClient.GenerateModLogoCollectionDirectoryPath(modId)
                   + "versionInfo.data");
        }

        /// <summary>Generatess the directory path for a mod gallery image collection.</summary>
        public static string GenerateModGalleryImageCollectionDirectoryPath(int modId)
        {
            return(Application.temporaryCachePath
                   + "/mod_images/"
                   + modId + "/");
        }

        /// <summary>Generates the file path for a mod galley image.</summary>
        public static string GenerateModGalleryImageFilePath(int modId,
                                                             string imageFileName,
                                                             ModGalleryImageSize size)
        {
            return(GenerateModGalleryImageCollectionDirectoryPath(modId)
                   + size.ToString() + "/"
                   + Path.GetFileNameWithoutExtension(imageFileName) +
                   ".png");
        }

        /// <summary>Retrieves the file paths for the mod logos in the cache.</summary>
        public static Dictionary<LogoSize, string> LoadModLogoFilePaths(int modId)
        {
            return CacheClient.ReadJsonObjectFile<Dictionary<LogoSize, string>>(CacheClient.GenerateModLogoVersionInfoFilePath(modId));
        }

        /// <summary>Stores a mod logo in the cache.</summary>
        public static void SaveModLogo(int modId, string fileName,
                                       LogoSize size, Texture2D logoTexture)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod logo without a mod id");
            Debug.Assert(!String.IsNullOrEmpty(fileName),
                         "[mod.io] Cannot cache a mod logo without file name as it"
                         + " is used for versioning purposes");

            string logoFilePath = CacheClient.GenerateModLogoFilePath(modId, size);
            CacheClient.WritePNGFile(logoFilePath, logoTexture);

            // - Version Info -
            var versionInfo = CacheClient.LoadModLogoFilePaths(modId);
            if(versionInfo == null)
            {
                versionInfo = new Dictionary<LogoSize, string>();
            }

            versionInfo[size] = fileName;
            CacheClient.WriteJsonObjectFile(GenerateModLogoVersionInfoFilePath(modId),
                                            versionInfo);
        }

        /// <summary>Retrieves a mod logo from the cache.</summary>
        public static Texture2D LoadModLogo(int modId, LogoSize size)
        {
            string logoFilePath = CacheClient.GenerateModLogoFilePath(modId, size);
            Texture2D logoTexture = CacheClient.ReadImageFile(logoFilePath);
            return(logoTexture);
        }

        /// <summary>Stores a mod gallery image in the cache.</summary>
        public static void SaveModGalleryImage(int modId,
                                               string imageFileName,
                                               ModGalleryImageSize size,
                                               Texture2D imageTexture)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod image without a mod id");

            string imageFilePath = CacheClient.GenerateModGalleryImageFilePath(modId,
                                                                               imageFileName,
                                                                               size);
            CacheClient.WritePNGFile(imageFilePath, imageTexture);
        }

        /// <summary>Retrieves a mod gallery image from the cache.</summary>
        public static Texture2D LoadModGalleryImage(int modId,
                                                    string imageFileName,
                                                    ModGalleryImageSize size)
        {
            string imageFilePath = CacheClient.GenerateModGalleryImageFilePath(modId,
                                                                               imageFileName,
                                                                               size);
            Texture2D imageTexture = CacheClient.ReadImageFile(imageFilePath);

            return(imageTexture);
        }

        // ---------[ MOD TEAM ]---------
        /// <summary>Generates the file path for a mod team's data.</summary>
        public static string GenerateModTeamFilePath(int modId)
        {
            return(CacheClient.GenerateModDirectoryPath(modId)
                   + "team.data");
        }

        /// <summary>Stores a mod team's data in the cache.</summary>
        public static void SaveModTeam(int modId,
                                       List<ModTeamMember> modTeam)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod team without a mod id");

            string filePath = CacheClient.GenerateModTeamFilePath(modId);
            CacheClient.WriteJsonObjectFile(filePath, modTeam);
        }

        /// <summary>Retrieves a mod team's data from the cache.</summary>
        public static List<ModTeamMember> LoadModTeam(int modId)
        {
            string filePath = CacheClient.GenerateModTeamFilePath(modId);
            var modTeam = CacheClient.ReadJsonObjectFile<List<ModTeamMember>>(filePath);
            return modTeam;
        }

        /// <summary>Deletes a mod team's data from the cache.</summary>
        public static void DeleteModTeam(int modId)
        {
            CacheClient.DeleteFile(CacheClient.GenerateModTeamFilePath(modId));
        }

        // ---------[ USERS ]---------
        /// <summary>Generates the file path for a user's profile.</summary>
        public static string GenerateUserProfileFilePath(int userId)
        {
            return(CacheClient.GetCacheDirectory()
                   + "users/"
                   + userId + ".data");
        }

        /// <summary>Stores a user's profile in the cache.</summary>
        public static void SaveUserProfile(UserProfile userProfile)
        {
            Debug.Assert(userProfile.id > 0,
                         "[mod.io] Cannot cache a user profile without a user id");

            string filePath = CacheClient.GenerateUserProfileFilePath(userProfile.id);
            CacheClient.WriteJsonObjectFile(filePath, userProfile);
        }

        /// <summary>Retrieves a user's profile from the cache.</summary>
        public static UserProfile LoadUserProfile(int userId)
        {
            string filePath = CacheClient.GenerateUserProfileFilePath(userId);
            var userProfile = CacheClient.ReadJsonObjectFile<UserProfile>(filePath);
            return(userProfile);
        }

        /// <summary>Iterates through all the user profiles in the cache.</summary>
        public static IEnumerable<UserProfileStub> IterateAllUserProfiles()
        {
            string profileDirectory = CacheClient.GetCacheDirectory() + "users/";

            if(Directory.Exists(profileDirectory))
            {
                string[] userFiles;
                try
                {
                    userFiles = Directory.GetFiles(profileDirectory);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read user profile directory."
                                          + "\nDirectory: " + profileDirectory + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    userFiles = new string[0];
                }

                foreach(string profileFilePath in userFiles)
                {
                    var profile = CacheClient.ReadJsonObjectFile<UserProfileStub>(profileFilePath);
                    if(profile != null)
                    {
                        yield return profile;
                    }
                }
            }
        }

        /// <summary>Deletes a user's profile from the cache.</summary>
        public static void DeleteUserProfile(int userId)
        {
            CacheClient.DeleteFile(CacheClient.GenerateUserProfileFilePath(userId));
        }
    }
}
