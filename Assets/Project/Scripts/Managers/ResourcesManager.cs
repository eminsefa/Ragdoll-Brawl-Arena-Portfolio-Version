using System.IO;
using Cysharp.Threading.Tasks;
using Project.Scripts.Network;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Project.Scripts.Managers
{
    //Responsible for addressables, assetbundles and ccd
    public class ResourcesManager : MonoBehaviour
    {
        public bool AreResourcesSet { get; private set; }

        private const string REMOTE_MANIFEST_URL = "https://github.com/eminsefa/Ragdoll-Brawl-Arena/raw/master/Assets/Project/VersionableObjects/ObjectVersionManifest.txt";
        private       string m_LocalVersionableObjectsPath;
        private       string m_LocalManifestPath;

        [SerializeField] AssetReference m_PlayerPrefabAsset;
        [SerializeField] AssetReference m_BotPrefabAsset;
        [SerializeField] AssetReference m_BoostPrefabAsset;
        [SerializeField] AssetReference m_EnvironmentPrefabAsset;
        [SerializeField] AssetReference m_GravityManagerAsset;
        [SerializeField] AssetReference m_RoomPlayerAsset;

        public GameObject PlayerPrefab      { get; private set; }
        public GameObject BotPrefab      { get; private set; }
        public GameObject BoostPrefab       { get; private set; }
        public GameObject EnvironmentPrefab { get; private set; }
        public GameObject GravityPrefab     { get; private set; }
        public RoomPlayer RoomPlayerPrefab  { get; private set; }

        private async void Awake()
        {
            // m_LocalVersionableObjectsPath = Path.Combine(Application.persistentDataPath, "VersionableObjects/");
            // m_LocalManifestPath           = Path.Combine(m_LocalVersionableObjectsPath,  "ObjectVersionManifest.txt");

            // await CheckAssetVersions();

            await Addressables.InitializeAsync();
            LoadAssets();

            DontDestroyOnLoad(gameObject);
            
        }

        [Button]
        private void LoadTextAssets()
        {
            var fontHandle = Addressables.LoadResourceLocationsAsync("fonts", typeof(ScriptableObject));
            fontHandle.Completed += fontLocations =>
                                    {
                                        foreach (IResourceLocation location in fontLocations.Result)
                                        {
                                            string locationPrimaryKey = location.PrimaryKey;
                                            Addressables.LoadAssetAsync<TMP_FontAsset>(locationPrimaryKey).Completed +=
                                                font =>
                                                {
                                                    MaterialReferenceManager.AddFontAsset(font.Result);
                                                };
                                        }
                                    };
        }

        private async UniTask CheckAssetVersions()
        {
            using (UnityWebRequest www = UnityWebRequest.Get(REMOTE_MANIFEST_URL))
            {
                await www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Application.Quit();
                    return;
                }

                string remoteManifestText = www.downloadHandler.text;

                if (File.Exists(m_LocalManifestPath))
                {
                    string localManifestText = await File.ReadAllTextAsync(m_LocalManifestPath);

                    string[] remoteLines = remoteManifestText.Split('\n');
                    string[] localLines  = localManifestText.Split('\n');

                    for (int i = 0; i < Mathf.Min(remoteLines.Length, localLines.Length); i++)
                    {
                        if (remoteLines[i].Contains(','))
                        {
                            string[] remoteInfo = remoteLines[i].Split(',');
                            string[] localInfo  = localLines[i].Split(',');

                            string assetID = remoteInfo[0];

                            int    remoteVersion = int.Parse(remoteInfo[1]);
                            string remotePath    = remoteInfo[2];

                            int localVersion = int.Parse(localInfo[1]);

                            if (localVersion < remoteVersion)
                            {
                                await DownloadAndReplaceAssetAsync(remotePath, assetID);
                                UpdateManifestText(assetID, remoteVersion);
                            }
                        }
                    }
                }
                else
                {
                    string directory = Path.GetDirectoryName(m_LocalManifestPath);
                    Directory.CreateDirectory(directory);

                    await File.WriteAllTextAsync(m_LocalManifestPath, remoteManifestText);

                    string[] remoteLines = remoteManifestText.Split('\n');

                    for (int i = 0; i < remoteLines.Length; i++)
                    {
                        if (remoteLines[i].Contains(','))
                        {
                            string[] remoteInfo = remoteLines[i].Split(',');
                            string   assetID    = remoteInfo[0];
                            string   remotePath = remoteInfo[2];

                            await DownloadAndReplaceAssetAsync(remotePath, assetID);
                        }
                    }
                }
            }
        }

        private async UniTask DownloadAndReplaceAssetAsync(string remotePath, string assetID)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(remotePath))
            {
                await www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Application.Quit();
                    return;
                }

                string assetPath = Path.Combine(m_LocalVersionableObjectsPath, assetID + ".prefab");

                if (File.Exists(assetPath))
                {
                    File.Delete(assetPath);
                }

                await File.WriteAllBytesAsync(assetPath, www.downloadHandler.data);
            }
        }

        private void UpdateManifestText(string assetID, int remoteVersion)
        {
            string[] lines = File.ReadAllLines(m_LocalManifestPath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(assetID + ","))
                {
                    string[] parts   = lines[i].Split(',');
                    int      version = int.Parse(parts[1]);
                    version = remoteVersion;

                    lines[i] = assetID + "," + version + "," + parts[2];
                    break;
                }
            }

            File.WriteAllLines(m_LocalManifestPath, lines);
        }


        private void LoadAssets()
        {
            DownloadRemoteAssets();
            
            m_PlayerPrefabAsset.LoadAssetAsync<GameObject>().Completed      += (operation) => { PlayerPrefab      = operation.Result; };
            m_BotPrefabAsset.LoadAssetAsync<GameObject>().Completed      += (operation) => { BotPrefab      = operation.Result; };
            m_BoostPrefabAsset.LoadAssetAsync<GameObject>().Completed       += (operation) => { BoostPrefab       = operation.Result; };
            m_GravityManagerAsset.LoadAssetAsync<GameObject>().Completed    += (operation) => { GravityPrefab     = operation.Result; };
            m_RoomPlayerAsset.LoadAssetAsync<GameObject>().Completed += (operation) =>
                                                                        {
                                                                            RoomPlayerPrefab = operation.Result.GetComponent<RoomPlayer>();
                                                                            AreResourcesSet  = true;
                                                                        };
        }

        private void DownloadRemoteAssets()
        {
            m_EnvironmentPrefabAsset.LoadAssetAsync<GameObject>().Completed += (operation) => { EnvironmentPrefab = operation.Result; };

        }

    }
}
  