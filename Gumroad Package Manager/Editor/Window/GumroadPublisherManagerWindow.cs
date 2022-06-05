using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Gumroad.API.Web;
using EMI.Utils;
using Unity.EditorCoroutines.Editor;
using Gumroad.API;
using System.IO;

namespace Gumroad.Window
{
    public class GumroadPublisherManagerWindow : EditorWindow
    {
        #region Properties
        GumroadAPIWeb gumroad = null;
        string save_download_path = null;
        string last_updated = "Never updated";

        #region Styles
        [SerializeField]
        protected GUIStyle sidePanels, leftPanel, rightPanel, product_title = null;
        GumroadWindowColors colors;
        Texture2D button_background_normal, button_background_active;
        #endregion

        #region Searching/Packages
        protected bool refreshing_listing = false;
        protected string filterString = null;
        #endregion

        #region PackageListing
        protected int toggled_product = -1;
        protected List<LibraryProduct> available_products = new List<LibraryProduct>();
        protected Vector2 listing_scrollbar;
        protected Texture product_img, author_icon = null;
        #endregion

        #region Product Viewing
        public class ProductView
        {
            public LibraryProduct library_product = new LibraryProduct();
            public List<ProductItem> versions = new List<ProductItem>();

            public ProductView() { }
            public ProductView(LibraryProduct library_product, List<ProductItem> versions)
            {
                this.library_product = library_product;
                this.versions.Clear();
                this.versions.AddRange(versions);
            }
        }
        protected ProductView viewing_product = null;
        protected Vector2 product_view_scrollbar = Vector2.zero;
        protected bool downloading_product_img = false;
        #endregion

        #region Classes
        public class CachedLibraryProducts
        {
            public List<LibraryProduct> cached = new List<LibraryProduct>();
        }
        #endregion

        #endregion

        #region Window
        [MenuItem("Window/Gumroad/Package Manager")]
        public static void OpenGumroadPackageManager()
        {
            EditorWindow window = EditorWindow.GetWindow<GumroadPublisherManagerWindow>("Gumroad Package Manager", focus: true);
            window.minSize = new Vector2(700, 480);
        }
        #endregion

        #region Initilization
        protected virtual void OnEnable()
        {
            // Set download path
            save_download_path = $"{Application.persistentDataPath}/../../Gumroad Package Manager";
            
            // Set UI Look
            colors = (GumroadWindowColors)AssetDatabase.LoadAssetAtPath("Assets/CBGames/Gumroad Package Manager/Editor/Window/WindowColors.asset", typeof(ScriptableObject));

            sidePanels = new GUIStyle();
            sidePanels.normal.background = EditorUtils.MakeTex(colors.side_background);
            sidePanels.normal.textColor = colors.text_color;
            
            leftPanel = new GUIStyle();
            leftPanel.normal.background = EditorUtils.MakeTex(colors.left_panel_background);
            leftPanel.normal.textColor = colors.text_color;

            rightPanel = new GUIStyle();
            rightPanel.normal.background = EditorUtils.MakeTex(colors.right_panel_background);
            rightPanel.normal.textColor = colors.text_color;

            try
            {
                product_title = new GUIStyle();
                product_title = new GUIStyle(EditorStyles.boldLabel);
                product_title.fontSize = 25;
            }
            catch 
            {
                this.StartCoroutine(SetBoldStyle());
            }

            button_background_normal = EditorUtils.MakeTex(colors.button_background);
            button_background_active = EditorUtils.MakeTex(colors.button_selected);

            // Initilize credentials & headers
            gumroad = ScriptableObject.CreateInstance<GumroadAPIWeb>();
            gumroad.Init((GumroadCredentials)AssetDatabase.LoadAssetAtPath("Assets/CBGames/Gumroad Package Manager/Editor/Authentication/GumroadCreds.asset", typeof(GumroadCredentials)));

            // Load cached data
            string found = EditorPrefs.GetString("CB_GUMROAD_cached_LibraryProducts");
            string updated = EditorPrefs.GetString("CB_GUMROAD_Last_Updated");
            if (!string.IsNullOrEmpty(found))
            {
                CachedLibraryProducts cached = new CachedLibraryProducts();
                cached = JsonUtility.FromJson<CachedLibraryProducts>(found);
                available_products.Clear();
                available_products.AddRange(cached.cached);
            }
            if (!string.IsNullOrEmpty(updated))
            {
                last_updated = updated;
            }

            //gumroad.GetProduct("16pwlTzyMSugZtMoT8qrEw==");
            //gumroad.GetProducts();
            //gumroad.GetProductsWebPage();
            //gumroad.GetDiscoverWebPage();
            //gumroad.GetCustomersPage();
            //gumroad.EditProduct("ICNXT");
            //gumroad.GetLibraryPage();
            // note product 7 is a google drive zip file
            //List<ProductItem> versions = gumroad.GetLibraryProductVersions(gumroad.cached_library_products[0].product_link);
            //gumroad.GumroadDownload(versions[0].download_url, Application.dataPath + $"/{versions[0].file_name}.{versions[0].extension.ToLower()}", versions[0].extension);
        }
        protected virtual IEnumerator SetBoldStyle()
        {
            yield return new WaitUntil(() => EditorStyles.boldLabel != null);
            product_title = new GUIStyle();
            product_title = new GUIStyle(EditorStyles.boldLabel);
            product_title.fontSize = 25;
        }
        #endregion

        #region Main Window
        protected virtual void OnGUI()
        {
            TopPanelLeft();
            TopPanelRight();
            LeftPanel();
            RightPanel();
            BottomPanelLeft();
            BottomPanelRight();
        }
        #endregion

        #region Panels
        protected virtual void LeftPanel()
        {
            GUILayout.BeginArea(new Rect(0, 21, 300, position.height-47), leftPanel);
            LeftPanelContents();
            GUILayout.EndArea();
            EditorGUI.DrawRect(new Rect(300, 21, 1, position.height - 47), colors.wireframe);
        }
        protected virtual void RightPanel()
        {
            GUILayout.BeginArea(new Rect(301, 21, position.width-301, position.height - 47), rightPanel);
            RightPanelContents();
            GUILayout.EndArea();
        }
        #region Bottom Panels
        protected virtual void BottomPanelLeft()
        {
            GUILayout.BeginArea(new Rect(0, position.height - 25, 300, 25), sidePanels);
            BottomPanelLeftContents();
            GUILayout.EndArea();
            EditorGUI.DrawRect(new Rect(0, position.height - 26, position.width, 1), colors.wireframe);
            EditorGUI.DrawRect(new Rect(300, position.height - 26, 1, 26), colors.wireframe);
        }
        protected virtual void BottomPanelRight()
        {
            GUILayout.BeginArea(new Rect(301, position.height - 25, position.width-301, 25), sidePanels);
            BottomPanelRightContents();
            GUILayout.EndArea();
        }
        #endregion

        #region Top Panels
        protected virtual void TopPanelLeft()
        {
            GUILayout.BeginArea(new Rect(0, 0, 350, 20), sidePanels);
            TopPanelLeftContents();
            GUILayout.EndArea();
            EditorGUI.DrawRect(new Rect(0, 20, position.width, 1), colors.wireframe);
        }
        protected virtual void TopPanelRight()
        {
            GUILayout.BeginArea(new Rect(351, 0, position.width-350, 20), sidePanels);
            TopPanelRightContents();
            GUILayout.EndArea();
            EditorGUI.DrawRect(new Rect(350, 0, 1, 20), colors.wireframe);
        }
        #endregion
        #endregion

        #region Contents
        #region Top Panel
        protected virtual void TopPanelLeftContents()
        {
            if (available_products.Count < 1)
                EditorGUILayout.LabelField("Open source package manager for Gumroad");
            else
                EditorGUILayout.LabelField($"Available Products: {available_products.Count}");
        }
        protected virtual void TopPanelRightContents()
        {
            filterString = GUILayout.TextField(filterString, GUI.skin.FindStyle("ToolbarSeachTextField"));
        }
        #endregion

        #region Bottom Panel
        protected virtual void BottomPanelLeftContents()
        {
            EditorGUILayout.BeginHorizontal();
            if (!gumroad.refreshing)
            {
                EditorGUILayout.LabelField(last_updated, GUILayout.Width(260));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh")))
                {
                    this.StartCoroutine(RefreshPackageList());
                }
            }
            else if (refreshing_listing == true)
            {
                EditorGUILayout.LabelField("Refreshing, please wait...");
            }
            EditorGUILayout.EndHorizontal();
        }
        protected virtual void BottomPanelRightContents()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open Download Folder"))
            {
                EditorUtility.RevealInFinder($"{save_download_path}/");
            }
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Right Panel
        protected virtual void RightPanelContents()
        {
            if (viewing_product == null || viewing_product.library_product == null) return;

            product_view_scrollbar = GUILayout.BeginScrollView(product_view_scrollbar, false, false);
            
            // Product Name
            EditorGUILayout.LabelField(viewing_product.library_product.product_name, product_title);
            
            // Author Info
            EditorGUILayout.BeginHorizontal();
            if (author_icon != null)
            {
                GUILayout.Box(author_icon, GUILayout.Width(25), GUILayout.Height(25));
            }
            EditorGUILayout.LabelField(viewing_product.library_product.author_name);
            EditorGUILayout.EndHorizontal();

            // Product & Author Url Button Links
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("View Product Page", GUILayout.Width(150)))
            {
                Application.OpenURL(viewing_product.library_product.product_link);
            }
            if (GUILayout.Button("View Author Page", GUILayout.Width(150)))
            {
                Application.OpenURL(viewing_product.library_product.author_library_link);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Product title image
            if (product_img != null)
            {
                GUILayout.Box(product_img, GUILayout.Width(300), GUILayout.Height(300));
            }
            else if (downloading_product_img)
            {
                EditorGUILayout.LabelField("Downloading image...");
            }

            // List available download versions
            if (viewing_product.versions.Count > 0)
            {
                EditorGUILayout.LabelField("Available Versions To Download:");
                foreach(ProductItem item in viewing_product.versions)
                {
                    if (item.is_downloading)
                    {
                        Rect r = EditorGUILayout.BeginVertical();
                        EditorGUI.ProgressBar(r, 0.1f, $"Downloading: {item.file_name}.{item.extension.ToLower()}");
                        GUILayout.Space(18);
                        EditorGUILayout.EndVertical();
                    }
                    else
                    {
                        EditorGUILayout.LabelField(item.description);
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Download"))
                        {
                            item.is_downloading = true;
                            string save_to_path = $"{save_download_path}/{viewing_product.library_product.author_name}/{viewing_product.library_product.product_name}/{item.file_name}.{item.extension.ToLower()}";
                            if (System.IO.File.Exists(save_to_path))
                            {
                                DownloadFinishedCallback(save_to_path);
                            }
                            else
                            {
                                gumroad.GumroadDownload(
                                    item.download_url,
                                    save_to_path,
                                    item.extension,
                                    DownloadFinishedCallback
                                );
                            }
                        }
                        EditorGUILayout.LabelField($"{item.file_name}.{item.extension.ToLower()}");
                        EditorGUILayout.LabelField(FileSizeToString(item.file_size));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndScrollView();
        }
        #endregion

        #region Left Panel
        protected virtual void LeftPanelContents()
        {
            if (refreshing_listing == false)
            {
                Texture2D org_background_norm = GUI.skin.button.normal.background;
                TextAnchor org_anchor = GUI.skin.button.alignment;

                GUI.skin.button.normal.background = button_background_normal;
                GUI.skin.button.active.background = button_background_active;
                GUI.skin.button.focused.background = button_background_active;
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;


                listing_scrollbar = GUILayout.BeginScrollView(listing_scrollbar);
                GUI.enabled = !gumroad.refreshing;
                for (int i = 0; i < available_products.Count; i++)
                {
                    if (toggled_product == i)
                        GUI.skin.button.normal.background = button_background_active;
                    else
                        GUI.skin.button.normal.background = button_background_normal;
                    if (toggled_product == i || string.IsNullOrEmpty(filterString) || available_products[i].product_name.ToLower().Contains(filterString.ToLower()))
                    {
                        if (GUILayout.Button(available_products[i].product_name, GUILayout.Width(280)))
                        {
                            toggled_product = i;
                            this.StartCoroutine(FetchProductDetails(available_products[i]));
                        }
                    }
                }
                GUI.enabled = true;
                GUILayout.EndScrollView();

                GUI.skin.button.normal.background = org_background_norm;
                GUI.skin.button.alignment = org_anchor;
            }
            else
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Refreshing list, please wait...");
                GUILayout.FlexibleSpace();
            }
        }
        #endregion
        #endregion

        #region Actions
        protected virtual IEnumerator RefreshPackageList()
        {
            refreshing_listing = true;
            List<LibraryProduct> tmp = gumroad.GetLibraryPage();
            if (tmp.Count > 0)
            {
                available_products.Clear();
                available_products.AddRange(tmp);
            }
            CachedLibraryProducts cached = new CachedLibraryProducts();
            cached.cached.AddRange(available_products);
            EditorPrefs.SetString("CB_GUMROAD_cached_LibraryProducts", JsonUtility.ToJson(cached));
            System.DateTime currentTime = System.DateTime.Now;
            last_updated = $"Last updated {currentTime.ToLongDateString()}, {currentTime.ToShortTimeString()}";
            EditorPrefs.SetString("CB_GUMROAD_Last_Updated", last_updated);
            refreshing_listing = false;
            yield return null;
        }
        protected virtual IEnumerator FetchProductDetails(LibraryProduct product)
        {
            yield return new WaitForSeconds(0.01f); // prevent a lockup in the editor
            
            // Setup product view page
            viewing_product = new ProductView();
            viewing_product.library_product = new LibraryProduct(product);

            // Background download icons, img
            product_img = null;
            author_icon = null;
            downloading_product_img = false;
            if (product.img_url != null)
            {
                downloading_product_img = true;
                gumroad.DownloadTexture(product.img_url, AssignProductImageTexture);
            }
            if (product.author_icon != null)
                gumroad.DownloadTexture(product.author_icon, AssignAuthorIconTexture);

            // Gather downloadable versions
            List<ProductItem> versions = gumroad.GetLibraryProductVersions(product.product_link);
            viewing_product.versions.Clear();
            viewing_product.versions.AddRange(versions);
            yield return null;
        }
        protected virtual void AssignProductImageTexture(Texture2D retrieved_img)
        {
            if (retrieved_img != null)
            {
                product_img = (Texture)retrieved_img;
            }
            downloading_product_img = false;
        }
        protected virtual void AssignAuthorIconTexture(Texture2D retrieved_img)
        {
            if (retrieved_img != null)
            {
                author_icon = (Texture)retrieved_img;
            }
        }
        protected virtual void DownloadFinishedCallback(string file_path)
        {
            string extension = Path.GetExtension(file_path).ToLower();
            string filename = Path.GetFileNameWithoutExtension(file_path).ToLower();
            ProductItem foundItem = viewing_product.versions.Find(x => x.file_name.ToLower() == filename.ToLower() && x.extension.ToLower() == extension.ToLower());
            if (foundItem != null)
                foundItem.is_downloading = false;
            if (extension == ".unitypackage")
            {
                if (EditorUtility.DisplayDialog("Download completed!", "Would you like to extract this into your project?", "Yes", "No"))
                {
                    AssetDatabase.ImportPackage(file_path, true);
                }
            }
            else if (extension == ".txt")
            {
                StreamReader reader = new StreamReader(file_path);
                string contents = reader.ReadToEnd();
                if (contents.ToLower().StartsWith("https://drive.google.com"))
                {
                    if (EditorUtility.DisplayDialog("Download completed!", $"This appears to be a google drive download link. Would you like to download the contents from this link?\n{contents}", "Yes", "No"))
                    {
                        string id = contents.Replace("https://drive.google.com/file/d/", "");
                        id = id.Split('/')[0];
                        if (foundItem != null)
                            foundItem.is_downloading = true;
                        this.StartCoroutine(gumroad.GoogleDriveDownload(id, file_path.Replace(Path.GetFileName(file_path), ""), DownloadFinishedCallback));
                    }
                    else if (EditorUtility.DisplayDialog("Copy?", "Would you like to copy this text file into your projects \"Assets/Gumroad\" directory?", "Yes", "No"))
                    {
                        CopyFileToProject(file_path);
                    }
                }
            }
            else if (EditorUtility.DisplayDialog("Download completed!", "This is not a unitypackage. Would you like to copy it to \"Assets/Gumroad\" in your project?", "Yes", "No"))
            {
                CopyFileToProject(file_path);
            }
        }
        protected virtual void CopyFileToProject(string file_path)
        {
            string dir_path = "Assets/Gumroad/";
            string filename = Path.GetFileNameWithoutExtension(file_path);
            string extension = Path.GetExtension(file_path);
            string newFullPath = file_path;

            if (!Directory.Exists(dir_path))
            {
                Directory.CreateDirectory(dir_path);
            }
            int count = 1;
            while (System.IO.File.Exists(newFullPath))
            {
                string tempFileName = string.Format("{0}({1})", filename, count++);
                newFullPath = Path.Combine(dir_path, tempFileName + extension);
            }
            System.IO.File.Copy(file_path, newFullPath, false);
            AssetDatabase.Refresh();
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(newFullPath, typeof(UnityEngine.Object));
            Selection.activeObject = obj;
        }
        #endregion

        #region Helpers
        protected virtual string FileSizeToString(long bytes)
        {
            // Gigabyte
            float value = Mathf.Round(((bytes / 1024f) / 1024f) / 1024);
            if (value < 1)
            {
                // Megabyte
                value = Mathf.Round((bytes / 1024f) / 1024f);
                if (value < 1)
                {
                    // Kilobytes
                    value = Mathf.Round(bytes / 1024f);
                    if (value < 1)
                    {
                        // Bytes
                        value = bytes;
                        return value.ToString() + "Bytes";
                    }
                    return value.ToString() + "KB";
                }
                return value.ToString() + "MB";
            }
            return value.ToString()+"GB";
        }
        #endregion
    }
}