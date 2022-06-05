using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gumroad.API.APIResponse
{
    [System.Serializable]
    public class SimpleResponse
    {
        public bool success = false;
        public string message = null;

        public SimpleResponse(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }
    }

    [System.Serializable]
    public class Products
    {
        public bool success = false;
        public List<Product> products = new List<Product>();
        public Product product = new Product();

        public Products(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }
    }

    [System.Serializable]
    public class Product
    {
        public string custom_permalink = null;
        public string custom_receipt = null;
        public string custom_summary = null;
        public List<string> custom_fields = new List<string>();
        public bool customizable_price = false;
        public string description = null;
        public bool deleted = false;
        public string max_purchase_count = null;
        public string name = null;
        public string preview_url = null;
        public bool require_shipping = false;
        public string subscription_duration = null;
        public bool published = false;
        public string url = null;
        public string id = null;
        public int price = 0;
        public string currency = "usd";
        public string short_url = null;
        public string thumbnail_url = null;
        public List<string> tags = new List<string>();
        public string formatted_price = "$1";
        public object file_info = null;
        public bool shown_on_profile = false;
        public int sales_count = 0;
        public int sales_usd_cents = 0;
        public bool is_tiered_membership = false;
        public string recurrences = null;
        public List<Variant> variants = new List<Variant>();

        public Product(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }
        public Product() { }
    }

    [System.Serializable]
    public class Variant
    {
        public string title = null;
        public List<VariantOption> options = new List<VariantOption>();
    }

    [System.Serializable]
    public class VariantOption
    {
        public string name = null;
        public int price_difference = 0;
        public bool is_pay_what_you_want = false;
    }
}