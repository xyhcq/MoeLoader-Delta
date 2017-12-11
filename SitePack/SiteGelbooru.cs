﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using HtmlAgilityPack;
using MoeLoaderDelta;
using System.Text.RegularExpressions;


namespace SitePack
{
    /// <summary>
    /// Gelbooru.com
    /// Fixed 171210
    /// </summary>
    class SiteGelbooru : AbstractImageSite
    {
        
        public override string SiteUrl { get { return "https://gelbooru.com"; } }
        public override string SiteName { get { return "gelbooru.com"; } }
        public override string ShortName { get { return "gelbooru"; } }
        public override string ShortType { get { return ""; } }
        public override bool IsSupportCount { get { return false; } }
        
        public override string GetPageString(int page, int count, string keyWord, IWebProxy proxy)
        {

            //string pageUrl = string.Format(SiteUrl + "/index.php?page=post&s=list&tags={0}&pid={1}", keyWord, (page-1)*42);
            string pageUrl;
            if (keyWord.Length == 0)
                pageUrl = string.Format(SiteUrl + "/index.php?page=post&s=list&pid={0}", (page - 1) * 42);
            else
                pageUrl = string.Format(SiteUrl + "/index.php?page=post&s=list&tags={0}&pid={1}", keyWord, (page - 1) * 42);
            MyWebClient client = new MyWebClient { Proxy = proxy, Encoding = Encoding.UTF8 };
            string pageString = client.DownloadString(pageUrl);
            client.Dispose();
            return pageString;
        }
        //tags https://gelbooru.com/index.php?page=tags&s=list&tags=kanto*
        public override List<Img> GetImages(string pageString, IWebProxy proxy)
        {
            List<Img> list = new List<Img>();
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(pageString);
            HtmlNodeCollection previewNodes = document.DocumentNode.SelectNodes("//div[@class=\"thumbnail-preview\"]");
            if (previewNodes == null)
                return list;
            foreach (HtmlNode node in previewNodes)
            {
                HtmlNode node1 = node.SelectSingleNode("./span/a");
                HtmlNode node2 = node1.SelectSingleNode("./img");
                string detailUrl = FormattedImgUrl(node1.Attributes["href"].Value);
                string desc = Regex.Match(node1.InnerHtml, "(?<=title=\" ).*?(?=  score)").Value;
                Img item = new Img()
                {
                    Desc = desc,
                    Tags = desc,
                    Id = Convert.ToInt32(Regex.Match(node1.Attributes["id"].Value, @"\d+").Value),
                    DetailUrl = detailUrl,
                    PreviewUrl = node2.Attributes["data-original"].Value
                    //PreviewUrl = node1.InnerHtml.Substring(node1.InnerHtml.IndexOf("original=\"") + 10,
                    //        node1.InnerHtml.IndexOf("\" src") - node1.InnerHtml.IndexOf("original=\"") - 10)
                };
                item.DownloadDetail = (i, p) =>
                {
                    string html = new MyWebClient { Proxy = p, Encoding = Encoding.UTF8 }.DownloadString(i.DetailUrl);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    HtmlNodeCollection liNodes = doc.DocumentNode.SelectNodes("//li");
                    HtmlNode imgData = doc.DocumentNode.SelectSingleNode("//*[@id=\"image\"]");
                    if (imgData != null)
                    {
                        i.Width = Convert.ToInt32(imgData.Attributes["data-original-width"].Value);
                        i.Height = Convert.ToInt32(imgData.Attributes["data-original-height"].Value);
                        i.SampleUrl = imgData.Attributes["src"].Value;
                    }
                    foreach (HtmlNode n in liNodes)
                    {
                        if (n.InnerText.Contains("Posted"))
                            i.Date = n.InnerText.Substring(n.InnerText.IndexOf("ed: ") + 3, n.InnerText.IndexOf(" by") - n.InnerText.IndexOf("d: ") - 3);
                        if (n.InnerHtml.Contains("by"))
                            i.Author = n.InnerText.Substring(n.InnerText.LastIndexOf(' ') + 1, n.InnerText.Length - n.InnerText.LastIndexOf(' ') - 1);
                        if (n.InnerText.Contains("Source"))
                            i.Source = n.SelectSingleNode("//*[@rel=\"nofollow\"]").Attributes["href"].Value;
                        if (n.InnerText.Contains("Rating") && n.InnerText.Contains("Safe"))
                            i.IsExplicit = false;
                        else if (n.InnerText.Contains("Rating"))
                            i.IsExplicit = true;
                        if (n.InnerText.Contains("Rating") && n.InnerText.Contains("Safe"))
                            i.IsExplicit = false;
                        else if (n.InnerText.Contains("Rating"))
                            i.IsExplicit = true;

                        if (n.InnerText.Contains("Score"))
                            i.Score = Convert.ToInt32(n.SelectSingleNode("./span").InnerText);
                        if (n.InnerHtml.Contains("Original"))
                        {
                            i.OriginalUrl = n.SelectSingleNode("./a").Attributes["href"].Value;
                            i.JpegUrl = n.SelectSingleNode("./a").Attributes["href"].Value;
                        }
                    }
                };
                list.Add(item);
            }
            return list;
        }
        private static string FormattedImgUrl(string prUrl)
        {
            try
            {
                if (prUrl.StartsWith("//"))
                    prUrl = "https:" + prUrl;
                if (prUrl.Contains("&amp;"))
                    prUrl = prUrl.Replace("&amp;", "&");
                return prUrl;

            }
            catch
            {
                return prUrl;
            }
        }
    }   
}
