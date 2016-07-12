using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ShareYourJourney.Controllers
{
    public class ShareController : Controller
    {
        public ActionResult PickAlbums()
        {
            if (TempData["tokenType"] == null && Request.Url != null)
                return new RedirectResult(Request.Url.Scheme + "://" + Request.Url.Host + "/Auth/Index?referrer=" + Request.Url.AbsoluteUri);

            var bla = TempData["tokenType"];
            var bli = TempData["accessToken"];

            //_picasaService.GetAlbums(TempData["tokenType"] + "", TempData["accessToken"] + "");

            //var picasaService = (PicasaService)TempData["PicasaService"];
            ////new PicasaRequest(new RequestSettings() {})
            //string uri = PicasaQuery.CreatePicasaUri("default");
            //AlbumQuery query = new AlbumQuery(uri);
            //query.Access = PicasaQuery.AccessLevel.AccessAll;
            //PicasaFeed feed = picasaService.Query(query);

            //var picasaViewModel = new PicasaViewModel()
            //{
            //    Albums = feed.Entries
            //};

            //foreach (PicasaEntry entry in feed.Entries)
            //{
            //    Album bla = null;
            //    //if (entry.IsAlbum)
            //    //    bla = (Album) entry;
            //    //entry.IsAlbum
            //    Console.WriteLine();
            //    AlbumAccessor ac = new AlbumAccessor(entry);
            //    ac.Access = "public";

            //    entry.Title.Text = "awd";

            //    entry.Update();
            //    Console.WriteLine(ac.NumPhotos);
            //}
            return null;
        }
    }
}