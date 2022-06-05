using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System.IO;
using System.Net;
using RestAPI;
using HtmlAgilityPack;
using Unity.EditorCoroutines.Editor;
using System.Collections;
using System;

// Requiement: https://www.nuget.org/packages/HtmlAgilityPack/
// convert to zip, extract, lib/ copy net version to plugins dir
// NOTE: by default I have included the .NET 2.0 version in this 
namespace Gumroad.API.Web
{
    public class GumroadAPIWeb : EditorWindow
    {
        public GumroadAPIWeb() { }
        public GumroadAPIWeb(GumroadCredentials credentials)
        {
            Init(credentials);
        }
        public virtual void Init(GumroadCredentials credentials)
        {
            try
            {
                this.credentials = credentials;
                if (string.IsNullOrEmpty(credentials.host_web) || string.IsNullOrEmpty(credentials.cookie))
                {
                    Debug.LogError("You have not set the 'Host_web' value or the 'cookie' value. These must be set in order to navigate gumroad properly");
                }
                api_headers.Clear();
                web_headers.Clear();
                api_headers.Add("Host", credentials.host_api);
                web_headers.Add("Host", credentials.host_web);
                web_headers.Add("cookie", credentials.cookie);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.Log("Failed to set credentials for the gumroad package manager. This could lead to other errors.");
            }
        }
        #region Properties
        RestAPI.RestAPI api = new RestAPI.RestAPI();
        Dictionary<string, string> api_headers = new Dictionary<string, string>();
        Dictionary<string, string> web_headers = new Dictionary<string, string>();
        [SerializeField, Tooltip("The gumroad credentials needed to interact with the gumroad API.")]
        protected GumroadCredentials credentials = null;
        public List<ProductWebProduct> cached_web_products = new List<ProductWebProduct>();
        //public List<Product> cached_api_products = new List<Product>();
        public List<Customer> cached_customers = new List<Customer>();
        public List<LibraryProduct> cached_library_products = new List<LibraryProduct>();
        public float download_percentage = 0;
        protected bool is_downloading = false;
        public bool refreshing = false;
        #endregion

        #region WebPage
        #region Downloading
        public virtual void DownloadTexture(string url, Action<Texture2D> callback = null)
        {
            this.StartCoroutine(E_DownloadTexture(url, callback));
        }
        protected virtual IEnumerator E_DownloadTexture(string url, Action<Texture2D> callback = null)
        { 
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();
            if (request.isHttpError || request.isNetworkError)
            {
                callback?.Invoke(null);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                callback?.Invoke(texture);
            }
        }

        /// <summary>
        /// Will do the multi-stage request in order to fully download a target package from gumroad at
        /// the provided download_link and save it to the save_path. It will also detect if the downloaded
        /// file is a simple text file. If it contains a google drive http link, it will auto follow it 
        /// and download that target file for you at the save_path. Finally when this fully completes the 
        /// download (whether from google drive or straight from gumroad) it will call the callback function
        /// with the final place where the file was downloaded to.
        /// </summary>
        /// <param name="download_link">The gumroad link to download the file.</param>
        /// <param name="save_path">The full path (including the file name) of where you want to save this downloaded file.</param>
        /// <param name="extension">The extension type this file is, helps to trigger another google drive download if txt type.</param>
        /// <param name="callback">The function to call that takes a string when the full download process has compelted.</param>
        public virtual void GumroadDownload(string download_link, string save_path, string extension, Action<string> callback = null)
        {
            if (!download_link.StartsWith("https://app.gumroad.com"))
                download_link = $"https://app.gumroad.com{download_link}";

            this.StartCoroutine(E_Download(download_link, save_path, extension, callback));
        }
        protected virtual IEnumerator E_Download(string download_link, string save_path, string extension, Action<string> callback = null)
        {
            refreshing = true;
            // First get the true download location
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(download_link);
            request.AllowAutoRedirect = false;
            request.Host = credentials.host_web;
            request.Headers.Add("cookie", credentials.cookie);
            string true_download_location = null;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string responseString = reader.ReadToEnd();
                reader.Close();
                true_download_location = response.Headers["Location"];
            }
            
            // If successfully retrieved the true download location, download the file
            if (!string.IsNullOrEmpty(true_download_location))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                UnityWebRequest webRequest = new UnityWebRequest(true_download_location, UnityWebRequest.kHttpVerbGET);
                
                // Send received bytes directory to this path (file name included)
                webRequest.downloadHandler = new DownloadHandlerFile(save_path);

                // Track % progress for downloading bar
                is_downloading = true;
                this.StartCoroutine(TrackDownloadProgress(webRequest));

                // Send this build out request to the endpoint
                yield return webRequest.SendWebRequest();

                // if there was any sort of error display it in the console for the end user
                if (webRequest.isNetworkError || webRequest.isHttpError)
                    Debug.LogError(webRequest.error);

                // Stop tracking the downloading process
                is_downloading = false;
                refreshing = false;
                callback?.Invoke(save_path);
                webRequest.downloadHandler.Dispose();
            }
            yield return null;
        }

        /// <summary>
        /// Will track the desired request and update the download progress.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        protected virtual IEnumerator TrackDownloadProgress(UnityWebRequest req)
        {
            while (is_downloading || download_percentage >= 100)
            {
                download_percentage = req.downloadProgress * 100;
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        /// <summary>
        /// Will extract the name of the file from the download link. Will download and save that filename
        /// to the desired directory then call an optional callback
        /// </summary>
        /// <param name="id">The google id of the file to download</param>
        /// <param name="save_dir">The directory to save the file to</param>
        /// <param name="callback">A callback that will receive the path to the saved file</param>
        public virtual IEnumerator GoogleDriveDownload(string id, string save_dir, Action<string> callback = null)
        {
            refreshing = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // Perform an unconfirmed download request to get the name of the file
            string filename = null;
            ResponseObject respObj = api.GET($"https://drive.google.com/uc?id={id}&export=download", new Dictionary<string, string>());
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(respObj.response);
            HtmlNode target_node = doc.DocumentNode.SelectSingleNode("//*[@id=\"uc-text\"]/p[2]/span/a/text()");
            filename = target_node.InnerText;

            if (filename != null)
            {
                string save_path = $"{save_dir}/{filename}";
                if (!System.IO.File.Exists(save_path))
                {
                    // download the file and save it to the extract filename
                    using (UnityWebRequest webRequest = new UnityWebRequest($"https://drive.google.com/uc?id={id}&export=download&confirm=t", UnityWebRequest.kHttpVerbGET))
                    {
                        // Send received bytes directory to this path (file name included)
                        webRequest.downloadHandler = new DownloadHandlerFile(save_path);

                        // Track % progress for downloading bar
                        is_downloading = true;
                        this.StartCoroutine(TrackDownloadProgress(webRequest));

                        // Send this build out request to the endpoint
                        yield return webRequest.SendWebRequest();

                        // if there was any sort of error display it in the console for the end user
                        if (webRequest.isNetworkError || webRequest.isHttpError)
                            Debug.LogError(webRequest.error);

                        // Stop tracking the downloading process
                        is_downloading = false;

                        callback?.Invoke(save_path);
                        webRequest.downloadHandler.Dispose();
                    }
                }
                callback?.Invoke(save_path);
            }
            refreshing = false;
        }
        #endregion

        #region Library Page Exploring
        /// <summary>
        /// Will list the available product versions that can be downloaded from the provided
        /// product link.
        /// </summary>
        /// <param name="product_link"></param>
        /// <returns>A list of product versions for download</returns>
        public virtual List<ProductItem> GetLibraryProductVersions(string product_link, Action<ProductVersions> callback = null)
        {
            refreshing = true;
            try
            {
                if (web_headers == null)
                    Debug.LogError("The web_headers have not been assigned!");
                ResponseObject respObj = api.GET(product_link, web_headers);
                if (respObj.code == HttpStatusCode.OK)
                {
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(respObj.response);
                    HtmlNode holder_div = doc.DocumentNode.SelectSingleNode("//*[@id=\"download-landing-page\"]/div/div[2]/div[2]");
                    string items_json = holder_div.Attributes["data-react-props"].Value;
                    ProductVersions product_versions = new ProductVersions(HtmlEntity.DeEntitize(items_json));
                    
                    refreshing = false;
                    callback?.Invoke(product_versions);
                    return product_versions.content_items;
                }
            }
            catch(Exception e)
            {
                Debug.LogError(e);
                Debug.Log($"Failed calling: {product_link}");
            }
            refreshing = false;
            callback?.Invoke(null);
            return new List<ProductItem>();
        }
        
        /// <summary>
        /// Will return all of the products that you have available for download on your own 
        /// personal library page.
        /// </summary>
        /// <returns>A list of products</returns>
        public virtual List<LibraryProduct> GetLibraryPage()
        {
            List<LibraryProduct> products = new List<LibraryProduct>();
            refreshing = true;
            try
            {
                ResponseObject respObj = api.GET($"https://app.gumroad.com/library", web_headers);
                if (respObj.code == HttpStatusCode.OK)
                {
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(respObj.response);
                    HtmlNode holder_div = doc.DocumentNode.SelectSingleNode("//*[@id=\"app\"]/div[4]/div/main/section/div/div[2]");
                    foreach (HtmlNode article in holder_div.ChildNodes)
                    {
                        // Extract product info & link
                        HtmlNode a = article.SelectSingleNode("a");
                        string product_link = a.Attributes["href"].Value;
                        string product_name = a.Attributes["aria-label"].Value;

                        // Extract image
                        string img_url = null;
                        try
                        {
                            HtmlNode img = a.SelectSingleNode("div/img");
                            img_url = img.Attributes["src"].Value;
                        }
                        catch { }

                        // Extract Author Link
                        HtmlNode footer_a = article.SelectSingleNode("footer/a");
                        string author_library_link = footer_a.Attributes["href"].Value;

                        // Extract author image & name
                        HtmlNode footer_img = footer_a.SelectSingleNode("img");
                        string author_icon = footer_img.Attributes["src"].Value;
                        string author_name = footer_a.InnerText;

                        // Save the product for adding to the cached list
                        products.Add(new LibraryProduct(
                            product_name,
                            product_link,
                            img_url,
                            author_library_link,
                            author_icon,
                            author_name
                        ));
                    }
                }
                if (products.Count > 0)
                {
                    cached_library_products.Clear();
                    cached_library_products.AddRange(products);
                }
            }
            catch(Exception e)
            {
                Debug.LogError(e);
                Debug.Log("An error occured while trying to get the library listing. This can occure if your cookie is expired or you have the wrong 'Host_web' value set.");
            }
            finally
            {
                refreshing = false;
            }
            return products;
        }
        #endregion

        #region Products Page Exploring
        /// <summary>
        /// Incomplete function, do not call.
        /// </summary>
        /// <param name="product_permalink"></param>
        public virtual void EditProduct(string product_permalink)
        {
            EditProduct pageContent = new EditProduct();
            ResponseObject respObj = api.GET($"https://app.gumroad.com/products/{product_permalink}/edit", web_headers);
            if (respObj.code == HttpStatusCode.OK)
            {
                Debug.Log(respObj.response);
                //StreamWriter writer = new StreamWriter("Assets/CBGames/Gumroad Publisher Manager/Editor/edit_product.html");
                //writer.WriteLine(respObj.response);
                //writer.Close();
            }
        }

        /// <summary>
        /// Will return stats about all of your products as well as individual information about each 
        /// individual product like links, icon_urls, sales, etc.
        /// </summary>
        /// <returns></returns>
        public virtual ProductWebPage GetProductsWebPage()
        {
            refreshing = true;
            ProductWebPage pageContent = new ProductWebPage();
            ResponseObject respObj = api.GET($"https://app.gumroad.com/products", web_headers);
            if (respObj.code == HttpStatusCode.OK)
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(respObj.response);
                
                ////// Stats /////
                // Extract Total Revenue Stats
                HtmlNode revenue_title = doc.DocumentNode.SelectSingleNode("//*[@id=\"app\"]/div[4]/main/div[1]/div/div/div/div[1]/section[1]/h2/text()[1]");
                HtmlNode revenue_amount = doc.DocumentNode.SelectSingleNode("//*[@id=\"app\"]/div[4]/main/div[1]/div/div/div/div[1]/section[1]/div/span");
                pageContent.stats.total_revenue_title = revenue_title.InnerText;
                pageContent.stats.total_revenue_amount = revenue_amount.InnerText;

                // Extract Total Customer Stats
                HtmlNode customers_title = doc.DocumentNode.SelectSingleNode("//*[@id=\"app\"]/div[4]/main/div[1]/div/div/div/div[1]/section[2]/h2/text()[1]");
                HtmlNode customers_amount = doc.DocumentNode.SelectSingleNode("//*[@id=\"app\"]/div[4]/main/div[1]/div/div/div/div[1]/section[2]/div/span");
                pageContent.stats.total_customers_title = customers_title.InnerText;
                pageContent.stats.total_customer_amount = customers_amount.InnerText;

                // Extract Active Members Stats
                HtmlNode members_title = doc.DocumentNode.SelectSingleNode("//*[@id=\"app\"]/div[4]/main/div[1]/div/div/div/div[1]/section[3]/h2/text()[1]");
                HtmlNode members_amount = doc.DocumentNode.SelectSingleNode("//*[@id=\"app\"]/div[4]/main/div[1]/div/div/div/div[1]/section[3]/div/span");
                pageContent.stats.total_active_members_title = members_title.InnerText;
                pageContent.stats.total_active_members_amount = members_amount.InnerText;

                // Extract MRR Stats
                HtmlNode mrr_title = doc.DocumentNode.SelectSingleNode("//*[@id=\"app\"]/div[4]/main/div[1]/div/div/div/div[1]/section[4]/h2/text()[1]");
                HtmlNode mrr_amount = doc.DocumentNode.SelectSingleNode("//*[@id=\"app\"]/div[4]/main/div[1]/div/div/div/div[1]/section[4]/div/span");
                pageContent.stats.mrr_title = mrr_title.InnerText;
                pageContent.stats.mrr_amount = mrr_amount.InnerText;

                /////// PRODUCTS ///////
                HtmlNode tbody_node = doc.DocumentNode.SelectSingleNode("//*[@id=\"app\"]/div[4]/main/div[1]/div/div/div/div[2]/section/table/tbody");
                cached_web_products.Clear();
                foreach (HtmlNode tr in tbody_node.ChildNodes)
                {
                    ProductWebProduct product = new ProductWebProduct();

                    // Extract product name & thumnail
                    HtmlNode img_node = tr.SelectSingleNode("td[1]/a/img");
                    product.icon_url = img_node.Attributes["src"].Value;
                    product.name = img_node.Attributes["alt"].Value;

                    // Extract full product link
                    HtmlNode link = tr.SelectSingleNode("td[2]/div/a[2]/small");
                    product.url = link.InnerText;

                    // Extract Sales
                    HtmlNode sales = tr.SelectSingleNode("td[3]/a");
                    product.sales = sales.InnerText;

                    // Extract Revenue
                    HtmlNode revenue = tr.SelectSingleNode("td[4]/text()");
                    product.revenue = revenue.InnerText;

                    // Extract Price
                    HtmlNode price = tr.SelectSingleNode("td[5]/text()");
                    product.price = price.InnerText;

                    // Extract Status
                    HtmlNode status = tr.SelectSingleNode("td[6]/span/small");
                    product.status = status.InnerText;

                    // Add product to page content
                    pageContent.products.Add(product);
                }
                cached_web_products.AddRange(pageContent.products);
            }

            refreshing = false;
            return pageContent;
        }
        
        /// <summary>
        /// Will tell you how many products are listed on the discover page, revenue from discover, and
        /// what sort of settings each product has on the discover gumroad page.
        /// </summary>
        /// <returns></returns>
        public virtual DiscoverWebPage GetDiscoverWebPage()
        {
            refreshing = true;
            DiscoverWebPage pageContent = new DiscoverWebPage();

            ResponseObject respObj = api.GET($"https://app.gumroad.com/products/discover", web_headers);
            if (respObj.code == HttpStatusCode.OK)
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(respObj.response);

                // Status
                HtmlNode discover_listed = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/main/div/div/section[1]/div/span");
                HtmlNode discover_revenue = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/main/div/div/section[2]/div/span");
                pageContent.stats.listed_on_discover = discover_listed.InnerText;
                pageContent.stats.revenue = discover_revenue.InnerText;

                // Product details
                HtmlNode tbody = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/main/div/section[2]/table/tbody");
                foreach(HtmlNode tr in tbody.ChildNodes)
                {
                    DiscoverWebProduct product = new DiscoverWebProduct();

                    // Extract icon & name
                    HtmlNode img = tr.SelectSingleNode("td[1]/a/img");
                    product.icon_url = img.Attributes["src"].Value;
                    product.name = img.Attributes["alt"].Value;

                    // Extract ratings
                    try
                    {
                        HtmlNode ratings = tr.SelectSingleNode("td[3]/span/span[1]");
                        product.ratings = string.IsNullOrEmpty(ratings.InnerText) ? "true" : ratings.InnerText;
                    }
                    catch
                    {
                        HtmlNode ratings = tr.SelectSingleNode("td[3]/span");
                        product.ratings = string.IsNullOrEmpty(ratings.InnerText) ? "true" : ratings.InnerText;
                    }

                    // Extract Visible Ratings
                    HtmlNode visible = tr.SelectSingleNode("td[4]/span");
                    product.visible_ratings = string.IsNullOrEmpty(visible.InnerText) ? "true" : visible.InnerText;

                    // Extract Safe For Work
                    HtmlNode sfw = tr.SelectSingleNode("td[5]/span");
                    product.sfw = string.IsNullOrEmpty(sfw.InnerText) ? "true" : sfw.InnerText;

                    // Extract Tags
                    HtmlNode tags = tr.SelectSingleNode("td[6]/span");
                    product.tags = string.IsNullOrEmpty(tags.InnerText) ? "true" : tags.InnerText;

                    // Extract Tags
                    HtmlNode status = tr.SelectSingleNode("td[7]/span/small");
                    product.status = status.InnerText;

                    // Add to page content
                    pageContent.products.Add(product);
                }
            }
            refreshing = false;
            return pageContent;
        }

        /// <summary>
        /// Will populate a list of customer across all of your products. This list will include
        /// information for each customer like purchase time, purchased product, email, license keys, etc.
        /// </summary>
        /// <returns>Return CustomersPage that holds a "customers" array of each Customer.</returns>
        public virtual CustomersPage GetCustomersPage()
        {
            refreshing = true;
            CustomersPage pageContent;
            ResponseObject respObj = api.GET($"https://app.gumroad.com/customers/sales?page=0&&active_customers_only=false", web_headers);
            if (respObj.code == HttpStatusCode.OK)
            {
                cached_customers.Clear();
                pageContent = new CustomersPage(respObj.response);
                cached_customers.AddRange(pageContent.customers);
                int additional = (pageContent.customers_count - 20) % 20 > 0 ? 1 : 0;
                int total_additional_calls = ((pageContent.customers_count - 20) / 20) + additional;
                int page = 0;
                while (page < total_additional_calls)
                {
                    page += 1;
                    ResponseObject subResponse = api.GET($"https://app.gumroad.com/customers/sales?page={page}&&active_customers_only=false", web_headers);
                    if (subResponse.code == HttpStatusCode.OK)
                    {
                        CustomersPage subContent = new CustomersPage(subResponse.response);
                        pageContent.customers.AddRange(subContent.customers);
                        cached_customers.AddRange(subContent.customers);
                    }
                }
                refreshing = false;
                return pageContent;
            }
            refreshing = false;
            return null;
        }
        #endregion
        #endregion

        //#region Products
        //#region Gets
        //public virtual Product GetProduct(string id)
        //{
        //    if (credentials == null)
        //    {
        //        Debug.LogError("Did not assign a GumroadCredentials object for this window to work.");
        //        return null;
        //    }

        //    if (credentials.token != null)
        //    {
        //        ResponseObject respObj = api.GET($"https://api.gumroad.com/v2/products/{id}?access_token={credentials.token}", api_headers);
        //        if (respObj.code == HttpStatusCode.OK)
        //        {
        //            Products api_products = new Products(respObj.response);
        //            return api_products.product;
        //        }
        //    }
        //    return null;
        //}
        //public virtual List<Product> GetProducts()
        //{
        //    if (credentials == null)
        //    {
        //        Debug.LogError("Did not assign a GumroadCredentials object for this window to work.");
        //        return new List<Product>();
        //    }

        //    //StreamReader reader = new StreamReader("Assets/CBGames/Gumroad Publisher Manager/Editor/example2.json");
        //    //string json = reader.ReadToEnd();
        //    //Products api_products = new Products(json);
        //    //cached_products.Clear();
        //    //cached_products.AddRange(api_products.products);

        //    if (credentials.token != null)
        //    {
        //        ResponseObject respObj = api.GET($"https://api.gumroad.com/v2/products?access_token={credentials.token}", api_headers);
        //        if (respObj.code == HttpStatusCode.OK)
        //        {
        //            Products api_products = new Products(respObj.response);
        //            cached_api_products.Clear();
        //            cached_api_products.AddRange(api_products.products);
        //            return cached_api_products;
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }

        //    return cached_api_products;
        //}
        //#endregion

        //#region Deletes
        //public virtual SimpleResponse DeleteProduct(string id)
        //{
        //    if (credentials != null)
        //    {
        //        ResponseObject respObj = api.DELETE($"https://api.gumroad.com/v2/products/{id}?access_token={credentials.token}", api_headers);
        //        if (respObj.code == HttpStatusCode.OK)
        //        {
        //            return new SimpleResponse(respObj.response);
        //        }
        //    }
        //    return null;
        //}
        //#endregion

        //#region Updates
        //public virtual Product EnableProduct(string id)
        //{
        //    if (credentials != null)
        //    {
        //        ResponseObject respObj = api.PUT($"https://api.gumroad.com/v2/products/{id}/enable?access_token={credentials.token}", api_headers);
        //        if (respObj.code == HttpStatusCode.OK)
        //        {
        //            Products api_products = new Products(respObj.response);
        //            return api_products.product;
        //        }
        //    }
        //    return null;
        //}
        //public virtual Product DisableProduct(string id)
        //{
        //    if (credentials != null)
        //    {
        //        ResponseObject respObj = api.PUT($"https://api.gumroad.com/v2/products/{id}/disable?access_token={credentials.token}", api_headers);
        //        if (respObj.code == HttpStatusCode.OK)
        //        {
        //            Products api_products = new Products(respObj.response);
        //            return api_products.product;
        //        }
        //    }
        //    return null;
        //} 
        //#endregion
        //#endregion

        #region Helpers
        /// <summary>
        /// Will convert things like ?myvalue=stuff&myothervalue=otherstuff into a dictionary
        /// of key-value pairs for the url parameters and return it.
        /// </summary>
        /// <param name="url">The url to parse out the parameters</param>
        /// <returns>Dictionary of parameters in the url</returns>
        protected Dictionary<string, string> ExtractURLParameters(string url)
        {
            if (!url.Contains("?"))return null;

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            string parameters_string = url.Split('?')[1];
            foreach(string parameter_set in parameters_string.Split('&'))
            {
                parameters.Add(parameter_set.Split('=')[0], parameter_set.Split('=')[1]);
            }
            return parameters;
        }
        
        /// <summary>
        /// Converts a byte array into a string, not currently used but might be helpful.
        /// </summary>
        /// <param name="val">The byte array</param>
        /// <returns>The string value</returns>
        protected string ByteArrayToString(byte[] val)
        {
            string b = "";
            int len = val.Length;
            for (int i = 0; i < len; i++)
            {
                if (i != 0)
                {
                    b += ",";
                }
                b += val[i].ToString();
            }
            return b;
        }
        #endregion
    }
}