using System;
using System.Collections.Generic;
using System.Text;

namespace Shazam.Database
{
    class MetaData
    {
        public string title { get; set; }
        public string artistsNames { get; set; }
        public string thumbnailM { get; set; }

        public string link { get; set; }

        public string mvLink { get; set; }

        
        public MetaData(string _title, string _artistNames,string _thumbnailM, string _link, string _mvLink)
        {
            this.title = _title;
            this.artistsNames = _artistNames;
            this.thumbnailM = _thumbnailM;
            this.link = _link;
            this.mvLink = _mvLink;
        }
    }
}
