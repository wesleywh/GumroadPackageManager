using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Gumroad.API.Web
{
    #region Product
    [System.Serializable]
    public class ProductVariant
    {
        public string id = null;
        public VariantRoot variant_root = new VariantRoot();
    }
    [System.Serializable]
    public class VariantRoot
    {
        public string title = null;
        public AvailableVariant selected_variant = new AvailableVariant();
    }
    [System.Serializable]
    public class AvailableVariant
    {
        public string id = null;
        public string name = null;
    }
    [System.Serializable]
    public class Card
    {
        public string visual = null;
        public string type = null;
        public string expiry_month = null;
    }
    #endregion

    #region Product Web Page
    [System.Serializable]
    public class ProductWebPage
    {
        public ProductsWebStats stats = new ProductsWebStats();
        public List<ProductWebProduct> products = new List<ProductWebProduct>();
    }

    [System.Serializable]
    public class ProductsWebStats
    {
        public string total_revenue_title = null;
        public string total_revenue_amount = null;
        public string total_customers_title = null;
        public string total_customer_amount = null;
        public string total_active_members_title = null;
        public string total_active_members_amount = null;
        public string mrr_title = null;
        public string mrr_amount = null;
    }

    [System.Serializable]
    public class ProductWebProduct
    {
        public string permalink = null;
        public string icon_url = null;
        public string url = null;
        public string name = null;
        public string sales = null;
        public string revenue = null;
        public string price = null;
        public string status = null;
    }
    #endregion

    #region Discover Web Page
    [System.Serializable]
    public class DiscoverWebPage
    {
        public DiscoverWebStats stats = new DiscoverWebStats();
        public List<DiscoverWebProduct> products = new List<DiscoverWebProduct>();
    }

    [System.Serializable]
    public class DiscoverWebStats
    {
        public string listed_on_discover = null;
        public string revenue = null;
    }

    [System.Serializable]
    public class DiscoverWebProduct
    {
        public string name = null;
        public string icon_url = null;
        public string product_link = null;
        public string ratings = null;
        public string visible_ratings = null;
        public string sfw = null;
        public string tags = null;
        public string status = null;
    }
    #endregion

    #region Customers Web Page
    [Serializable]
    public class CustomersPage
    {
        public List<Customer> customers = new List<Customer>();
        public int customers_count = 0;

        public CustomersPage(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }
        public CustomersPage() { }
    }

    [Serializable]
    public class Customer
    {
        public string id = null;
        public string email = null;
        public string seller_id = null;
        public string timestamp = null;
        public string daystamp = null;
        public string created_at = null;
        public string product_name = null;
        public string product_has_variants = null;
        public string price = null;
        public string gumroad_fee = null;
        public string formatted_display_price = null;
        public string formatted_total_price = null;
        public string currency_symbol = null;
        public string amount_refundable_in_currency = null;
        public string product_id = null;
        public string product_permalink = null;
        public string partially_refunded = null;
        public string chargedback = null;
        public string purchase_email = null;
        public string state = null;
        public string zip_code = null;
        public string country = null;
        public bool paid = false;
        public bool has_variants = false;
        public List<ProductVariant> variants = new List<ProductVariant>();
        public string variants_and_quantity = null;
        public bool has_custom_fields = false;
        public string order_id = null;
        public bool is_recurring_billing = false;
        public bool can_contact = false;
        public bool is_following = false;
        public bool disputed = false;
        public bool dispute_won = false;
        public bool is_additional_contribution = false;
        public bool discover_fee_charged = false;
        public bool is_upgrade_purchase = false;
        public bool is_gift_sender_purchase = false;
        public bool is_gift_receiver_purchase = false;
        public string referrer = null;
        public string product_rating = null;
        public string reviews_count = null;
        public string average_rating = null;
        public string receipt_url = null;
        public bool can_ping = false;
        public string license_key = null;
        public string license_id = null;
        public bool license_disabled = false;
        public int quantity = 0;

        public Customer(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }
        public Customer() { }
    }
    #endregion

    #region Edit Product
    // Top level class used to submit changes to a product on gumroad
    [Serializable]
    public class EditProduct
    {
        public List<GumroadContent> link = new List<GumroadContent>();
    }
    [Serializable]
    public class GumroadContent
    {
        public List<File> files = new List<File>();
        public List<Folder> folders = new List<Folder>();
        public List<string> tags = new List<string>();
        public List<string> shipping_destinations = new List<string>();
        public List<OfferCode> offer_codes = new List<OfferCode>();
        public Dictionary<string, int> preview_positions = new Dictionary<string, int>();
        public List<Variant> variants = new List<Variant>();
        public bool stream_only = false;
        public bool pdf_stamp_enabled = false;
        public string delivery = "gumroad";
        public string name = null;
        public string taxonomy_id = null;
        public string description = null;
        public string i_want_this_button_text_option = null;
        public int is_licensed = 1;
        public int limit_sales = 0;
        public int quantity_enabled = 1;
        public int should_show_sales_count = 0;
        public string custom_view_content_button_text = "Get Purchased Content!";
        public string custom_receipt = "";
        public int require_shipping = 0;
        public int is_epublication = 0;
        public string custom_permalink = "";
        public int is_adult = 0;
        public int display_product_reviews = 1;
        public int shown_on_profile = 1;
        public string custom_summary = "";
        public string custom_attributes = "";
        public string custom_fields = "";
        public int should_display_offer_field_on_purchase_form = 1;
        public int remove_offer_field_from_all_products = 0;
        public int add_offer_field_to_all_products_purchase_form = 1;
        public string price_range = "0+";
        public string purchase_type = "";
        public bool customizable_price = true;

    }
    [Serializable]
    public class File
    {
        public string external_id = "";
        public string display_name = "";
        public string description = "";
        public string folder_id = "";
        public string url = "";
        public string position = "";
        public string size = null;
        public string extension = "UNITYPACKAGE";
    }
    [Serializable]
    public class Folder
    {
        public string id = "";
        public string name = "";
    }
    [Serializable]
    public class OfferCode
    {
        public string id = "";
        public string type = "";
        public string amount = "";
        public int max_purchase_count = 1;
        public string name = "";
        public bool universal = false;
    }
    [Serializable]
    public class Variant
    {
        public string name = "";
        public string id = "";
        public List<VariantOption> options = new List<VariantOption>();
    }
    [Serializable]
    public class VariantOption
    {
        public string id = "";
        public string name = "";
        public string description = "";
        public List<VariantIntegration> integrations = new List<VariantIntegration>();
        public string product_file_ids = "";
        public string product_unsaved_url_ids = "";
        public string price = "";

    }
    [Serializable]
    public class VariantIntegration
    {
        public int circle = 0;
        public int discord = 0;

    }
    #endregion

    #region Library
    [Serializable]
    public class LibraryProduct
    {

        public string product_name = null;
        public string product_link = null;
        public string img_url = null;
        public string author_library_link = null;
        public string author_icon = null;
        public string author_name = null;

        public LibraryProduct() { }
        public LibraryProduct(LibraryProduct product)
        {
            this.product_name = product.product_name;
            this.product_link = product.product_link;
            this.img_url = product.img_url;
            this.author_library_link = product.author_library_link;
            this.author_icon = product.author_icon;
            this.author_name = product.author_name;
        }
        public LibraryProduct(string product_name, string product_link, string img_url, string author_library_link, string author_icon, string author_name)
        {
            this.product_link = product_link;
            this.product_name = product_name;
            this.img_url = img_url;
            this.author_library_link = author_library_link;
            this.author_icon = author_icon;
            this.author_name = author_name;
        }
    }
    [Serializable]
    public class ProductVersions
    {
        public List<ProductItem> content_items = new List<ProductItem>();
        public ProductVersions(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }
    }
    [Serializable]
    public class ProductItem
    {
        public string type = null;
        public string file_name = null;
        public string description = null;
        public string extension = null;
        public int file_size = 0;
        public string pagelength = null;
        public string duration = null;
        public string id = null;
        public string download_url = null;
        public string stream_url = null;
        public string audio_params = null;
        public string kindle_data = null;
        public string latest_media_location = null;
        public string content_length = null;
        public string read_url = null;
        public string external_link_url = null;
        public string subtitle_files = null;
        public bool is_downloading = false;
    }
    #endregion
}