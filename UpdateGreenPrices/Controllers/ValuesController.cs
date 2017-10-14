using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using HtmlAgilityPack;
using UpdateGreenPrices.Models;
using System.Data.Entity;

namespace UpdateGreenPrices.Controllers
{
  public class ValuesController : ApiController
  {
    private HtmlAgilityPack.HtmlDocument FWebSite;
    static private string FWebSiteURL = "http://greenmarkets.com.au/";

    bool NextNodeSTC = false;
    double STCPrice = 0.0;
    bool NextNodeLGC = false;
    double LGCPrice = 0.0;
    bool NextNodeVEEC = false;
    double VEECPrice = 0.0;
    bool NextNodeESC = false;
    double ESCPrice = 0.0;
    DateTime _CostEffectiveDate;

    public IHttpActionResult Get()
    {
#region RetrieveFromWebLocalCode
      void GetWebsiteHTML(string pURL)
      {
        string getString(string uri)
        {
          var httpClient = new HttpClient();

          return httpClient.GetStringAsync(uri).Result;
        }

        FWebSite = null;
        FWebSite = new HtmlAgilityPack.HtmlDocument();

        if (NetworkInterface.GetIsNetworkAvailable())
          FWebSite.LoadHtml(getString(pURL));
        else
          ConnectionLost();
      }
      async void ConnectionLost()
      {
        //ContentDialog noNetworkDialog = new ContentDialog()
        //{
        //  Title = "No Network connection",
        //  Content = "Check connection and try again.",
        //  PrimaryButtonText = "Ok"
        //};

        //await noNetworkDialog.ShowAsync();
      }
      void RetrieveValues(HtmlAgilityPack.HtmlDocument pWebSite)
      {
        void DrillHtml(HtmlNode pContent)
        {
          if (pContent.HasChildNodes)
            foreach (HtmlNode item in pContent.ChildNodes)
              switch (item.InnerText)
              {
                case "STCs":
                  //The next Node will be the STC Price 
                  NextNodeSTC = true;
                  break;

                case "LGCs":
                  NextNodeLGC = true;
                  break;

                case "VEECs":
                  NextNodeVEEC = true;
                  break;

                case "ESCs":
                  NextNodeESC = true;
                  break;

                default:
                  if ((NextNodeSTC) && (item.Name == "span"))
                  {
                    STCPrice = Convert.ToDouble(item.InnerText.Replace("$", ""));
                    NextNodeSTC = false;
                  }
                  else
                    if ((NextNodeLGC) && (item.Name == "span"))
                  {

                    LGCPrice = Convert.ToDouble(item.InnerText.Replace("$", ""));
                    NextNodeLGC = false;
                  }
                  else
                    if ((NextNodeVEEC) && (item.Name == "span"))
                  {

                    VEECPrice = Convert.ToDouble(item.InnerText.Replace("$", ""));
                    NextNodeVEEC = false;
                  }
                  else
                    if ((NextNodeESC) && (item.Name == "span"))
                  {

                    ESCPrice = Convert.ToDouble(item.InnerText.Replace("$", ""));
                    NextNodeESC = false;
                  }
                  else
                  if (item.GetAttributeValue("class", "") == "valid-from")
                    _CostEffectiveDate = Convert.ToDateTime(item.InnerText.Replace("Closing prices as at: ", "").Replace("  ", "").Replace("\n", "").Replace("\t", "").Replace(" ", "/"));
                  else
                    DrillHtml(item);
                  break;
              }
        }

        try
        {


          if (pWebSite != null && pWebSite.DocumentNode != null)
          {
            //DrillHtml(pWebSite.DocumentNode);

            HtmlNode content = pWebSite.GetElementbyId("schema-prices");

            DrillHtml(content);

            foreach (HtmlNode child in content.ChildNodes)
            {
              if (child.Name == "div")
                foreach (HtmlNode item in child.ChildNodes)
                {
                  if (item.Name == "div")
                  {
                    if (item.GetAttributeValue("class", "") == "schema-prices")
                    {
                      foreach (HtmlNode contentItem in item.ChildNodes)
                        if (contentItem.Name == "p" && contentItem.FirstChild.Name == "a")
                        {
                          //    string url = contentItem.FirstChild.FirstChild.GetAttributeValue("src", "");
                          //    string title = contentItem.FirstChild.FirstChild.GetAttributeValue("title", "");

                          //    AddContent(title, url);
                        }
                    }
                  }
                  else
                    if (item.Name == "schema-prices")
                  {
                    foreach (HtmlNode contentItem in item.ChildNodes)
                      if (contentItem.Name == "p" && contentItem.FirstChild.Name == "a")
                      {
                        //    string url = contentItem.FirstChild.FirstChild.GetAttributeValue("src", "");
                        //    string title = contentItem.FirstChild.FirstChild.GetAttributeValue("title", "");

                        //    AddContent(title, url);
                      }
                  }
                }
            }
          }
        }
        finally
        {

        }

      }
#endregion

      //Get Values from website
      GetWebsiteHTML(FWebSiteURL);
      RetrieveValues(FWebSite);

      Database.SetInitializer<CostGreenContext>(new CreateDatabaseIfNotExists<CostGreenContext>());

      using (var db = new CostGreenContext())
      {
        db.Database.Initialize(true);

        if (Convert.ToInt32(db.GreenCosts.Count()) == 0)
          SeedGreenCosts(db);

        if (db.GreenCosts.Any(e => e.EffectiveDate == _CostEffectiveDate && e.EndDate == new DateTime(9999, 12, 31)))
        { }// Nothing to do, we already have this data 
        else
        {
          //Update the previous week's end date
          var query = from data in db.GreenCosts where data.EndDate == new DateTime(9999, 12, 31) select data;

          foreach (GreenCosts details in query)
            details.EndDate = _CostEffectiveDate.AddDays(-1);

          //Add the details from the Website
          db.GreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = _CostEffectiveDate, EndDate = new DateTime(9999, 12, 31), Version = 1, VersionDate = DateTime.Now, Price = STCPrice });
          db.GreenCosts.Add(new GreenCosts() { CertificateType = "LGC", AuthorId = -1, EffectiveDate = _CostEffectiveDate, EndDate = new DateTime(9999, 12, 31), Version = 1, VersionDate = DateTime.Now, Price = LGCPrice });
          db.GreenCosts.Add(new GreenCosts() { CertificateType = "ESC", AuthorId = -1, EffectiveDate = _CostEffectiveDate, EndDate = new DateTime(9999, 12, 31), Version = 1, VersionDate = DateTime.Now, Price = ESCPrice });
          db.GreenCosts.Add(new GreenCosts() { CertificateType = "VEEC", AuthorId = -1, EffectiveDate = _CostEffectiveDate, EndDate = new DateTime(9999, 12, 31), Version = 1, VersionDate = DateTime.Now, Price = VEECPrice });

          db.SaveChanges();
        }
      }
      //EnergyRetailProfit  - Call the AzureDB this
      return Ok();
    }

    private void SeedGreenCosts(CostGreenContext db)
    {
      IList<GreenCosts> defaultGreenCosts = new List<GreenCosts>();

      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 01, 13), EndDate = new DateTime(2017, 01, 19), Version = 1, VersionDate = DateTime.Now, Price = 40.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 01, 20), EndDate = new DateTime(2017, 01, 26), Version = 1, VersionDate = DateTime.Now, Price = 40.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 01, 27), EndDate = new DateTime(2017, 02, 02), Version = 1, VersionDate = DateTime.Now, Price = 40.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 02, 03), EndDate = new DateTime(2017, 02, 09), Version = 1, VersionDate = DateTime.Now, Price = 40.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 02, 10), EndDate = new DateTime(2017, 02, 16), Version = 1, VersionDate = DateTime.Now, Price = 40.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 02, 17), EndDate = new DateTime(2017, 02, 23), Version = 1, VersionDate = DateTime.Now, Price = 40.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 02, 24), EndDate = new DateTime(2017, 03, 02), Version = 1, VersionDate = DateTime.Now, Price = 40.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 03, 03), EndDate = new DateTime(2017, 03, 09), Version = 1, VersionDate = DateTime.Now, Price = 40.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 03, 10), EndDate = new DateTime(2017, 03, 16), Version = 1, VersionDate = DateTime.Now, Price = 40.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 03, 17), EndDate = new DateTime(2017, 03, 23), Version = 1, VersionDate = DateTime.Now, Price = 39.50 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 03, 24), EndDate = new DateTime(2017, 03, 30), Version = 1, VersionDate = DateTime.Now, Price = 39.20 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 03, 31), EndDate = new DateTime(2017, 04, 06), Version = 1, VersionDate = DateTime.Now, Price = 39.20 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 04, 07), EndDate = new DateTime(2017, 04, 13), Version = 1, VersionDate = DateTime.Now, Price = 39.30 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 04, 14), EndDate = new DateTime(2017, 04, 20), Version = 1, VersionDate = DateTime.Now, Price = 39.30 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 04, 21), EndDate = new DateTime(2017, 04, 27), Version = 1, VersionDate = DateTime.Now, Price = 39.30 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 04, 28), EndDate = new DateTime(2017, 05, 04), Version = 1, VersionDate = DateTime.Now, Price = 39.30 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 05, 05), EndDate = new DateTime(2017, 05, 11), Version = 1, VersionDate = DateTime.Now, Price = 39.30 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 05, 12), EndDate = new DateTime(2017, 05, 18), Version = 1, VersionDate = DateTime.Now, Price = 39.30 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 05, 19), EndDate = new DateTime(2017, 05, 25), Version = 1, VersionDate = DateTime.Now, Price = 39.30 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 05, 26), EndDate = new DateTime(2017, 06, 01), Version = 1, VersionDate = DateTime.Now, Price = 38.90 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 06, 02), EndDate = new DateTime(2017, 06, 08), Version = 1, VersionDate = DateTime.Now, Price = 38.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 06, 09), EndDate = new DateTime(2017, 06, 15), Version = 1, VersionDate = DateTime.Now, Price = 38.90 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 06, 16), EndDate = new DateTime(2017, 06, 22), Version = 1, VersionDate = DateTime.Now, Price = 39.40 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 06, 23), EndDate = new DateTime(2017, 06, 29), Version = 1, VersionDate = DateTime.Now, Price = 39.20 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 06, 30), EndDate = new DateTime(2017, 07, 06), Version = 1, VersionDate = DateTime.Now, Price = 37.50 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 07, 07), EndDate = new DateTime(2017, 07, 13), Version = 1, VersionDate = DateTime.Now, Price = 38.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 07, 14), EndDate = new DateTime(2017, 07, 20), Version = 1, VersionDate = DateTime.Now, Price = 35.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 07, 21), EndDate = new DateTime(2017, 07, 27), Version = 1, VersionDate = DateTime.Now, Price = 29.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 07, 28), EndDate = new DateTime(2017, 08, 03), Version = 1, VersionDate = DateTime.Now, Price = 30.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 08, 04), EndDate = new DateTime(2017, 08, 10), Version = 1, VersionDate = DateTime.Now, Price = 30.50 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 08, 11), EndDate = new DateTime(2017, 08, 17), Version = 1, VersionDate = DateTime.Now, Price = 31.20 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 08, 18), EndDate = new DateTime(2017, 08, 27), Version = 1, VersionDate = DateTime.Now, Price = 31.45 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 08, 28), EndDate = new DateTime(2017, 09, 07), Version = 1, VersionDate = DateTime.Now, Price = 30.80 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 09, 08), EndDate = new DateTime(2017, 09, 19), Version = 1, VersionDate = DateTime.Now, Price = 31.30 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 09, 20), EndDate = new DateTime(2017, 09, 21), Version = 1, VersionDate = DateTime.Now, Price = 33.30 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 09, 22), EndDate = new DateTime(2017, 10, 04), Version = 1, VersionDate = DateTime.Now, Price = 34.75 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "STC", AuthorId = -1, EffectiveDate = new DateTime(2017, 10, 05), EndDate = new DateTime(9999, 12, 31), Version = 1, VersionDate = DateTime.Now, Price = 36.75 });

      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "LGC", AuthorId = -1, EffectiveDate = new DateTime(2017, 08, 11), EndDate = new DateTime(2017, 08, 17), Version = 1, VersionDate = DateTime.Now, Price = 84.80 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "LGC", AuthorId = -1, EffectiveDate = new DateTime(2017, 08, 18), EndDate = new DateTime(2017, 08, 27), Version = 1, VersionDate = DateTime.Now, Price = 85.00 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "LGC", AuthorId = -1, EffectiveDate = new DateTime(2017, 08, 28), EndDate = new DateTime(2017, 09, 07), Version = 1, VersionDate = DateTime.Now, Price = 85.25 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "LGC", AuthorId = -1, EffectiveDate = new DateTime(2017, 09, 08), EndDate = new DateTime(2017, 09, 19), Version = 1, VersionDate = DateTime.Now, Price = 85.25 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "LGC", AuthorId = -1, EffectiveDate = new DateTime(2017, 09, 20), EndDate = new DateTime(2017, 09, 21), Version = 1, VersionDate = DateTime.Now, Price = 84.50 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "LGC", AuthorId = -1, EffectiveDate = new DateTime(2017, 09, 22), EndDate = new DateTime(2017, 10, 04), Version = 1, VersionDate = DateTime.Now, Price = 82.25 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "LGC", AuthorId = -1, EffectiveDate = new DateTime(2017, 10, 05), EndDate = new DateTime(9999, 12, 31), Version = 1, VersionDate = DateTime.Now, Price = 84.15 });

      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "ESC", AuthorId = -1, EffectiveDate = new DateTime(2017, 08, 11), EndDate = new DateTime(2017, 08, 17), Version = 1, VersionDate = DateTime.Now, Price = 15.75 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "ESC", AuthorId = -1, EffectiveDate = new DateTime(2017, 08, 18), EndDate = new DateTime(2017, 08, 27), Version = 1, VersionDate = DateTime.Now, Price = 16.20 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "ESC", AuthorId = -1, EffectiveDate = new DateTime(2017, 08, 28), EndDate = new DateTime(2017, 09, 07), Version = 1, VersionDate = DateTime.Now, Price = 14.20 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "ESC", AuthorId = -1, EffectiveDate = new DateTime(2017, 09, 08), EndDate = new DateTime(2017, 09, 19), Version = 1, VersionDate = DateTime.Now, Price = 14.60 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "ESC", AuthorId = -1, EffectiveDate = new DateTime(2017, 09, 20), EndDate = new DateTime(2017, 09, 21), Version = 1, VersionDate = DateTime.Now, Price = 16.50 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "ESC", AuthorId = -1, EffectiveDate = new DateTime(2017, 09, 22), EndDate = new DateTime(2017, 10, 04), Version = 1, VersionDate = DateTime.Now, Price = 17.40 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "ESC", AuthorId = -1, EffectiveDate = new DateTime(2017, 10, 05), EndDate = new DateTime(9999, 12, 31), Version = 1, VersionDate = DateTime.Now, Price = 20.50 });

      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "VEEC", AuthorId = -1, EffectiveDate = new DateTime(2017, 08, 11), EndDate = new DateTime(2017, 08, 17), Version = 1, VersionDate = DateTime.Now, Price = 13.30 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "VEEC", AuthorId = -1, EffectiveDate = new DateTime(2017, 08, 18), EndDate = new DateTime(2017, 08, 27), Version = 1, VersionDate = DateTime.Now, Price = 13.50 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "VEEC", AuthorId = -1, EffectiveDate = new DateTime(2017, 08, 28), EndDate = new DateTime(2017, 09, 07), Version = 1, VersionDate = DateTime.Now, Price = 17.75 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "VEEC", AuthorId = -1, EffectiveDate = new DateTime(2017, 09, 08), EndDate = new DateTime(2017, 09, 19), Version = 1, VersionDate = DateTime.Now, Price = 17.75 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "VEEC", AuthorId = -1, EffectiveDate = new DateTime(2017, 09, 20), EndDate = new DateTime(2017, 09, 21), Version = 1, VersionDate = DateTime.Now, Price = 18.25 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "VEEC", AuthorId = -1, EffectiveDate = new DateTime(2017, 09, 22), EndDate = new DateTime(2017, 10, 04), Version = 1, VersionDate = DateTime.Now, Price = 18.25 });
      defaultGreenCosts.Add(new GreenCosts() { CertificateType = "VEEC", AuthorId = -1, EffectiveDate = new DateTime(2017, 10, 05), EndDate = new DateTime(9999, 12, 31), Version = 1, VersionDate = DateTime.Now, Price = 17.00 });

      foreach (GreenCosts item in defaultGreenCosts)
        db.GreenCosts.Add(item);

      db.SaveChanges();
    }
  }
}
